using System;
using System.Linq;

namespace virtual_autoclicker_console
{
    class Program
    {
        static void Main()
        {
            ConsoleHelper.WriteMessage("Virtual Autoclicker Console is starting!");

            VacEnvironment.Initialize();
            StartClosingHandlers();
            Console.Title = "Virtual Autoclicker";

            ConsoleHelper.WriteMessage("Virtual Autoclicker Console has started!");

            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    Console.Write("VAC >> ");
                    var Input = Console.ReadLine();

                    if (Input.Length > 0)
                    {
                        CommandHandler.ParseCommand(Input.Split(' ')[0], Input.Split(' ').Length > 1 ? Input.Split(' ').Skip(1).ToArray() : null);
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
