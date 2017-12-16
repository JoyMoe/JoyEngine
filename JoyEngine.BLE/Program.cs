using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using MoonSharp.Interpreter;

namespace JoyEngine.BLE
{
    class Program
    {
        private static int _ticksCount;
        private static int _ticksLimit;
        private static readonly ConcurrentBag<string> _ticks = new ConcurrentBag<string>();

        static void Main(string[] args)
        {
            var basePath = Environment.CurrentDirectory;
            var scriptPath = $"{basePath}/scripts";
            var tickScriptPath = $"{scriptPath}/ticks";

            Directory.CreateDirectory(tickScriptPath);

            Script.WarmUp();

            var Configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT")}.json", optional: true)
                .Build();

            var files = Directory.GetFiles(tickScriptPath, "*.lua");
            foreach (var file in files)
            {
                _ticks.Add(File.ReadAllText(file));
            }

            if (_ticks.Count > 0)
            {
                var timer = new Timer((state) =>
                {
                    if (_ticksCount >= _ticksLimit) return;
                    foreach (var tick in state as ConcurrentBag<string>)
                    {
                        try
                        {
                            Script.RunString(tick);
                        }
                        catch (ScriptRuntimeException ex)
                        {
                            Console.WriteLine(ex.DecoratedMessage);
                            Environment.Exit(1);
                        }
                    }
                    _ticksCount += 1;
                }, _ticks, Timeout.Infinite, Timeout.Infinite);

                _ticksLimit = 16;
                if (!string.IsNullOrWhiteSpace(Configuration["Ticks"]))
                {
                    _ticksLimit = Int32.Parse(Configuration["Ticks"]);
                }

                timer.Change(0, 1000 / _ticksLimit);
            }

            while (true)
            {
                _ticksCount = 0;
                Thread.Sleep(1000);
            }
        }
    }
}
