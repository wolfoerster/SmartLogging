using System;
using System.Threading;
using SmartLogging;

namespace TestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Press [Esc] to quit ...");

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

            SmartLogger.Flush();
        }
    }
}
