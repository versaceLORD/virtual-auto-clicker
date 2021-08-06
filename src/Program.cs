using System;
using System.Linq;

namespace VirtualAutoClicker
{
    internal static class Program
    {
        private static void Main()
        {
            ConsoleHelper.WriteMessage("Virtual Autoclicker Console is starting!");

            VacEnvironment.Initialize();

            StartClosingHandlers();
            Console.Title = "Virtual Autoclicker";

            ConsoleHelper.WriteMessage("Virtual Autoclicker Console has started!\n\r");

            while (VacEnvironment.Active)
            {
                if (!(Console.ReadKey(true).Key is ConsoleKey.Enter))
                {
                    continue;
                }
                
                Console.Write("VAC >> ");

                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Length <= 0)
                {
                    continue;
                }
                    
                var command = VacEnvironment.GetCommand(input.Split(' ')[0]);
                if (command is null)
                {
                    ConsoleHelper.WriteWarning($"No command found named '{command}'");
                    continue;
                }
                
                command.Execute(input.Split(' ').Skip(1).ToArray());
            }
        }

        /// <summary>
        /// Starts all handlers ensuring that the application closes the running autoclicker before exiting.
        /// </summary>
        private static void StartClosingHandlers()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                ConsoleHelper.WriteWarning("Application closing, running clean up!");
                e.Cancel = true;
                VacEnvironment.GetAcWorker()?.Picnic();
                VacEnvironment.Close();
                Environment.Exit(0);
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => VacEnvironment.GetAcWorker()?.Picnic();
        }
    }
}
