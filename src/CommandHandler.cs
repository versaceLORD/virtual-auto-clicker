using System;
using System.Text.RegularExpressions;
using VirtualAutoClicker.Console.Enums;
using VirtualAutoClicker.Console.Models;

namespace VirtualAutoClicker.Console
{
    public static class CommandHandler
    {
        public static void ParseCommand(string commandValue, string[]? args)
        {
            if (string.IsNullOrWhiteSpace(commandValue))
            {
                return;
            }

            var acWorker = VacEnvironment.GetAcWorker();
            if (acWorker is null)
            {
                ConsoleHelper.WriteError("AutoClickerWorker was not properly initialized, please restart the application.");

                return;
            }

            try
            {
                Enum.TryParse<Commands>(commandValue, false, out var command);

                switch (command)
                {
                    case Commands.Start:
                    case Commands.Startautoclicker:
                        {
                            if (args is null || args.Length <= 2)
                            {
                                ConsoleHelper.WriteWarning(
                                    "Command usage: 'startautoclicker \"P\" X,Y I' please " +
                                    "refer to the readme.md file for further assistance.");

                                break;
                            }

                            var processName = args[0];

                            // If the process name is put between double quotation marks
                            if (args[0].StartsWith('"') && args[0].EndsWith('"'))
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
                                X = int.Parse(args[1].Split(',')[0]),
                                Y = int.Parse(args[1].Split(',')[1]),
                            };

                            StartAutoClicker(acWorker, processName, coordinates, int.Parse(args[2]));

                            break;
                        }
                    case Commands.Stop:
                    case Commands.StopAutoClicker:
                    case Commands.Picnic:
                        {
                            Picnic(acWorker);

                            break;
                        }
                    case Commands.Unknown:
                    //TODO do something regarding unknow commands.

                    default:
                        ConsoleHelper.WriteWarning($"No command found named '{commandValue}'");

                        break;
                }
            }
            catch (Exception exception)
            {
                ConsoleHelper.WriteError($"Problem parsing or starting command '{commandValue}'", exception);
            }
        }

        private static void StartAutoClicker(AutoClickerWorker clickerWorker, string processName, Coordinates coordinates, int interval)
        {
            var autoClicker = new AutoClicker
            {
                Active = true,
                Coordinates = coordinates,
                Interval = interval,
                ProcessName = processName,
            };

            try
            {
                autoClicker.Init();

                clickerWorker.AutoClicker = autoClicker;

                ConsoleHelper.WriteMessage("Autoclicker started!");
            }
            catch (Exception exception)
            {
                clickerWorker.Picnic();

                ConsoleHelper.WriteError("Something went wrong when trying to start the autoclicker!", exception);
            }
        }

        /// <summary>
        /// Closes the started autoclicker
        /// </summary>
        /// <param name="clickerWorker"></param>
        private static void Picnic(AutoClickerWorker clickerWorker)
        {
            clickerWorker.Picnic();

            ConsoleHelper.WriteMessage("Autoclicker stopped!");
        }
    }
}
