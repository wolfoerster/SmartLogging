using System;
using System.Threading;
using System.Threading.Tasks;
using SmartLogging;

namespace TestApp;

internal class Program
{
    private static readonly SmartLogger Log = new();
    private static readonly CancellationTokenSource TokenSource = new();

    static void Main(string[] args)
    {
        Console.WriteLine("Press [Esc] to quit ...");

        LogWriter.Init();
        LogWriter.MinimumLogLevel = LogLevel.Verbose;

        Task.Run(() => Method1(111, TokenSource.Token));
        Task.Run(() => Method1(3333, TokenSource.Token));

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
        LogWriter.Exit();
    }

    private static void DoSomething(string name, int age)
    {
        Log.Information($"name={name},age={age}");
        Log.Information(new { name, age });
    }

    private static void Method1(int i, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var level = ++i % 7;
            Log.Write(i, (LogLevel)level);
            Thread.Sleep(30 + level);
        }
    }
}
