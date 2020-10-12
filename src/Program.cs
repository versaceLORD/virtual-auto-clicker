using System;
using System.Linq;

namespace VirtualAutoClicker
{
    class Program
    {
        static void Main()
        {
            ConsoleHelper.WriteMessage("Virtual Autoclicker Console is starting!");

            VacEnvironment.Initialize();

            StartClosingHandlers();
            Console.Title = "Virtual Autoclicker";

            ConsoleHelper.WriteMessage("Virtual Autoclicker Console has started!\n\r");

            while (true)
            {
                if (Console.ReadKey(true).Key is ConsoleKey.Enter)
                {
                    Console.Write("VAC >> ");

                    var input = Console.ReadLine();

                    if (!string.IsNullOrWhiteSpace(input) && input.Length > 0)
                    {
                        CommandHandler.ParseCommand(
                            input.Split(' ')[0],
                            input.Split(' ').Length > 1 ? input.Split(' ').Skip(1).ToArray() : null
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Starts all handlers ensuring that the application closes the running autoclicker before exiting.
        /// </summary>
        public static void StartClosingHandlers()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                ConsoleHelper.WriteWarning("Application closing, running clean up!");
                e.Cancel = true;
                VacEnvironment.GetAcWorker()?.Picnic();
                Environment.Exit(0);
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                VacEnvironment.GetAcWorker()?.Picnic();
            };
        }
    }
}
