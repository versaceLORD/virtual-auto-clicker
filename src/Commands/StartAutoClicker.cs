using System;
using System.Text.RegularExpressions;

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

            // If the process name is put between double quotation marks
            if (arguments[0].StartsWith('"') && arguments[0].EndsWith('"'))
            {
                var pattern = new Regex("\"(.*?)\"");
                var matches = pattern.Matches(processName);
                if (matches.Count > 0)
                {
                    processName = matches[0].Groups[1].Value.Replace("\"", "");
                }
            }

            var coordinates = new Coordinates
            {
                X = int.Parse(arguments[1].Split(',')[0]),
                Y = int.Parse(arguments[1].Split(',')[1]),
            };

            // Instantiate the new autoclicker
            Start(
                proposedAcName,
                processName,
                coordinates,
                int.Parse(arguments[2])
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
                acWorker?.AutoClickers?.Add(acName, ac);
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
