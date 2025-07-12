using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SmartLogging;

namespace TestApp;

internal class Program
{
    private static readonly SmartLogger Log = new();
    private static readonly CancellationTokenSource TokenSource = new();

    static void Main()
    {
        Console.WriteLine("Press [Esc] to quit ...");

        try
        {
            // check what happens when file name is invalid
            LogWriter.Init("A:\\*.log");
        }
        catch (ArgumentException ex)
        {
            Debug.WriteLine(ex);
        }

        var logStream = new MemoryStream();
        var settings = new LogSettings
        {
            MinimumLogLevel = LogLevel.Verbose,
            LogToFile = true,
            LogToConsole = true,
            LogStream = logStream,
        };
        LogWriter.Init(settings);

        Log.Information("this message is for you");
        Task.Run(() => Method1(111));
        Task.Run(() => Method1(3333));

        while (true)
        {
            var keyInfo = Console.ReadKey();
            Log.Information(keyInfo);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                break;
            }
        }

        try
        {
            var innerException = new ArgumentException("this is a bad argument", "badarg");
            throw new AggregateException("this is an AggregateException", innerException);
        }
        catch (Exception exception)
        {
            Log.Warning(exception.ToString());
        }

        DoSomething("asd", 123);
        Log.Information(DateTime.Now);

        TokenSource.Cancel();
        LogWriter.Flush();

        var logs = Encoding.UTF8.GetString(logStream.ToArray());
    }

    private static void DoSomething(string name, int age)
    {
        Log.Information($"name={name},age={age}");
        Log.Information(new { name, age });
    }

    private static void Method1(int i)
    {
        while (!TokenSource.Token.IsCancellationRequested)
        {
            var level = ++i % 7;
            Log.Write(i, (LogLevel)level);
            Thread.Sleep(30 + level);
        }
    }
}
