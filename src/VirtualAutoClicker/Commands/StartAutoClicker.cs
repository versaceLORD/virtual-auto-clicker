using System;

using VirtualAutoClicker.Models;

namespace VirtualAutoClicker.Commands
{
    public class StartAutoClicker : ICommand
    {
        public void Execute(string[] arguments)
        {
            if (arguments is null || arguments.Length <= 2)
            {
                ConsoleHelper.WriteWarning("Command usage: 'startautoclicker \"P\" X,Y I N' please refer to the readme.md file for further assistance.");
                return;
            }

            var proposedAcName = string.Empty;
            if (arguments.Length >= 4)
            {
                proposedAcName = arguments[3];
            }

            var processName = arguments[0];
            
            var xConversion = int.TryParse(arguments[1].Split(',')[0], out var x);
            var yConversion = int.TryParse(arguments[1].Split(',')[1], out var y);
            if (!xConversion || !yConversion)
            {
                ConsoleHelper.WriteWarning("Command usage: 'startautoclicker \"P\" X,Y I N' please refer to the readme.md file for further assistance.");
                return;
            }
            
            var coordinates = new Coordinates
            {
                X = x,
                Y = y,
            };

            var intervalConversion = int.TryParse(arguments[2], out var interval);
            if (!intervalConversion)
            {
                ConsoleHelper.WriteWarning("Command usage: 'startautoclicker \"P\" X,Y I N' please refer to the readme.md file for further assistance.");
                return;
            }

            // Instantiate the new autoclicker
            Start(
                proposedAcName,
                processName,
                coordinates,
                interval
            );
        }

        private static void Start(
            string proposedAcName, 
            string processName, 
            Coordinates coordinates,
            int interval
        )
        {
            var acWorker = VacEnvironment.GetAcWorker();
            var acName = proposedAcName;
            if (string.IsNullOrWhiteSpace(acName))
            {
                acName = Guid.NewGuid().ToString();
            }

            var ac = new AutoClicker
            {
                Name = acName,
                Active = true,
                Coordinates = coordinates,
                Interval = interval,
                ProcessName = processName,
            };

            try
            {
                acWorker?.AutoClickers?.TryAdd(acName, ac);
                ac.Init();

                ConsoleHelper.WriteMessage($"Autoclicker '{acName}' started!");
            }
            catch (Exception exc)
            {
                // Something went wrong, ensure the autoclicker worker doesn't keep track of this instance anymore.
                if (acWorker is { } && acName is { })
                {
                    acWorker?.RemoveAc(acName);
                }

                ConsoleHelper.WriteError("Something went wrong when trying to start the autoclicker!", exc);
            }
        }
    }
}
