using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                var message = QueueMessage.Parser.ParseFrom(ea.Body);
                if (message.Status == Constants.MessageStatus.Dropped) return;

                _sockets.TryGetValue(message.Package.To, out var webSocket);
                if (webSocket == null || webSocket.State != WebSocketState.Open) return;

                await SendMessageAsync(webSocket, message.Package);
            };

            _channel.BasicConsume(queue: queueName,
                                  autoAck: true,
                                  consumer: consumer);
        }

        private IConfiguration Configuration { get; }

        private static IModel _channel;
        private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

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

                        var socketId = Guid.NewGuid().ToString();
                        _sockets.TryAdd(socketId, currentSocket);

                        while (true)
                        {
                            if (context.RequestAborted.IsCancellationRequested)
                            {
                                break;
                            }

                            var ioMessage = await ReceiveMessageAsync(currentSocket, context.RequestAborted);

                            var message = new QueueMessage
                            {
                                Package = ioMessage,
                                Status = Constants.MessageStatus.Received
                            };

                            _channel.BasicPublish(exchange: Constants.MessageStatus.Received,
                                                  routingKey: "",
                                                  basicProperties: null,
                                                  body: message.ToByteArray());
                        }

                        _sockets.TryRemove(socketId, out var dummy);

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

        private static Task SendMessageAsync(WebSocket socket, IMessage message, CancellationToken ct = default(CancellationToken))
        {
            var segment = new ArraySegment<byte>(message.ToByteArray());
            return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
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

                return result.MessageType != WebSocketMessageType.Binary ? null : IoMessage.Parser.ParseFrom(ms);
            }
        }
    }
}
