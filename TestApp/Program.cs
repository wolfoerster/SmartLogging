using System;
using SmartLogging;

namespace TestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press [Esc] to quit ...");

            LogWriter.Init();
            var logger = new SmartLogger();

            while (true)
            {
                var keyInfo = Console.ReadKey();
                logger.Information(keyInfo);

                if (keyInfo.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            LogWriter.Exit();
        }
    }
}
