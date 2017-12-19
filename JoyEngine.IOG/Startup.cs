using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using JoyEngine.Common.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JoyEngine.IOG
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(Configuration["AMQP:Uri"])
            };

            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();

            _channel.ExchangeDeclare(exchange: Constants.MessageStatus.Received,
                                     type: ExchangeType.Direct);
            _channel.ExchangeDeclare(exchange: Constants.MessageStatus.Approved,
                                     type: ExchangeType.Fanout);

            var queueName = _channel.QueueDeclare().QueueName;

            _channel.QueueBind(queue: queueName,
                               exchange: Constants.MessageStatus.Approved,
                               routingKey: "");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var stream = new MemoryStream(ea.Body);
                var message = Serializer.Deserialize<QueueMessage>(stream);
                if (message.Status == Constants.MessageStatus.Dropped) return;

                var target = message.To;

                switch (target.Substring(0, 2))
                {
                    case "s:":
                        _sessions.TryGetValue(message.To, out var webSocket);

                        await SendMessageAsync(webSocket, message);

                        break;
                    case "g:":
                        _groups.TryGetValue(message.To, out var sessions);

                        foreach (var session in sessions)
                        {
                            _sessions.TryGetValue(session, out var socket);
                            await SendMessageAsync(socket, message);
                        }

                        break;
                    case "i:":
                        if (target == Constants.MessageTarget.IOG)
                            ReceiveInternalControlMessage(message);
                        break;
                    default:
                        return;
                }
            };

            _channel.BasicConsume(queue: queueName,
                                  autoAck: true,
                                  consumer: consumer);
        }

        private IConfiguration Configuration { get; }

        private static IModel _channel;
        private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> _groups = new ConcurrentDictionary<string, ConcurrentBag<string>>();
        private static readonly ConcurrentDictionary<string, WebSocket> _sessions = new ConcurrentDictionary<string, WebSocket>();

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var currentSocket = await context.WebSockets.AcceptWebSocketAsync();

                        var socketId = $"s:{Convert.ToBase64String(Guid.NewGuid().ToByteArray())}";
                        _sessions.TryAdd(socketId, currentSocket);

                        while (true)
                        {
                            if (context.RequestAborted.IsCancellationRequested)
                            {
                                break;
                            }

                            var ioMessage = await ReceiveMessageAsync(currentSocket, context.RequestAborted);

                            var message = new QueueMessage(ioMessage) {
                                Status = Constants.MessageStatus.Received
                            };

                            using (var stream = new MemoryStream())
                            {
                                Serializer.Serialize(stream, message);
                                _channel.BasicPublish(exchange: Constants.MessageStatus.Received,
                                                      routingKey: "",
                                                      basicProperties: null,
                                                      body: stream.ToArray());
                            }
                        }

                        _sessions.TryRemove(socketId, out var dummy);

                        await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, currentSocket.CloseStatusDescription, context.RequestAborted);
                        currentSocket.Dispose();
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });

            app.UseMvcWithDefaultRoute();
        }

        private static Task SendMessageAsync(WebSocket socket, IoMessage message, CancellationToken ct = default(CancellationToken))
        {
            if (socket == null || socket.State != WebSocketState.Open) return Task.FromCanceled(ct);

            using (var stream = new MemoryStream()) {
                Serializer.Serialize(stream, message);
                stream.TryGetBuffer(out var buffer);
                return socket.SendAsync(buffer, WebSocketMessageType.Binary, true, ct);
            }
        }

        private static async Task<IoMessage> ReceiveMessageAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    ct.ThrowIfCancellationRequested();

                    result = await socket.ReceiveAsync(buffer, ct);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                return result.MessageType != WebSocketMessageType.Binary ? null : Serializer.Deserialize <IoMessage>(ms);
            }
        }

        private static void ReceiveInternalControlMessage(QueueMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
