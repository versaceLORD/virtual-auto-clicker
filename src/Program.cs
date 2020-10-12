using System;
using System.Linq;

namespace VirtualAutoClicker.Console
{
    class Program
    {
        static void Main()
        {
            ConsoleHelper.WriteMessage("Virtual Autoclicker Console is starting!");

            VacEnvironment.Initialize();

            StartClosingHandlers();
            System.Console.Title = "Virtual Autoclicker";

            ConsoleHelper.WriteMessage("Virtual Autoclicker Console has started!\n\r");

            while (true)
            {
                if (System.Console.ReadKey(true).Key is ConsoleKey.Enter)
                {
                    System.Console.Write("VAC >> ");

                    var input = System.Console.ReadLine();

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
            System.Console.CancelKeyPress += (sender, e) =>
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
