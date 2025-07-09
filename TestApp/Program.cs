using System;
using SmartLogging;

namespace TestApp;

internal class Program
{
    private static SmartLogger Log = new();

    static void Main(string[] args)
    {
        Console.WriteLine("Press [Esc] to quit ...");

        LogWriter.Init();

        while (true)
        {
            var keyInfo = Console.ReadKey();
            Log.Information(keyInfo);

            if (keyInfo.Key == ConsoleKey.Escape)
            {
                break;
            }
        }

        DoSomething("asd", 123);
        Log.Information(DateTime.Now);
        LogWriter.Exit();
    }

    private static void DoSomething(string name, int age)
    {
        Log.Information($"name={name},age={age}");
        Log.Information(new { name, age });
    }
}
