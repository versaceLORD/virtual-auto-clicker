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

                var inputCommand = input.Split(' ')[0];
                var arguments = ConsoleHelper.GetInputArguments(input.Split(' ').Skip(1).ToList());
                
                ConsoleHelper.WriteMessage($"{inputCommand} - {arguments[0]} - {arguments[1]} - {arguments[2]}");
                
                var command = VacEnvironment.GetCommand(inputCommand);
                if (command is null)
                {
                    ConsoleHelper.WriteWarning($"No command found named '{inputCommand}'");
                    continue;
                }
                
                command.Execute(arguments);
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
