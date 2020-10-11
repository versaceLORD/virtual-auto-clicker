using System;
using System.Text.RegularExpressions;

namespace virtual_autoclicker_console
{
    /// <summary>
    /// Written in lower case since all commands are entered in lowercase.
    /// </summary>
    public enum Commands
    {
        unknown,
        startautoclicker,
        start,
        stop,
        stopautoclicker,
        picnic,
    }

    public static class CommandHandler
    {
        public static void ParseCommand(string commandStr, string[]? args)
        {
            if (string.IsNullOrWhiteSpace(commandStr))
                return;

            var acWorker = VacEnvironment.GetAcWorker();
            if (acWorker == null)
            {
                ConsoleHelper.WriteError("AutoClickerWorker was not properly initialized, please restart the application.");
                return;
            }

            try
            {
                Enum.TryParse<Commands>(commandStr, out var command);
                switch (command)
                {
                    case Commands.start:
                    case Commands.startautoclicker:
                        {
                            if (args == null || args.Length <= 2)
                            {
                                ConsoleHelper.WriteWarning("Command usage: 'startautoclicker \"P\" X,Y I' please refer to the readme.md file for further assistance.");
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
                    case Commands.stop:
                    case Commands.stopautoclicker:
                    case Commands.picnic:
                        {
                            Picnic(acWorker);
                            break;
                        }
                    case Commands.unknown:
                    default:
                        ConsoleHelper.WriteWarning($"No command found named '{commandStr}'");
                        break;
                }


            }
            catch (Exception exc)
            {
                ConsoleHelper.WriteError($"Problem parsing or starting command '{commandStr}'", exc);
            }
        }

        static void StartAutoClicker(AutoClickerWorker acWorker, string processName, Coordinates coordinates, int interval)
        {
            var ac = new AutoClicker
            {
                Active = true,
                Coordinates = coordinates,
                Interval = interval,
                ProcessName = processName,
            };

            try
            {
                ac.Init();
                acWorker.AutoClicker = ac;
                ConsoleHelper.WriteMessage("Autoclicker started!");
            }
            catch (Exception exc)
            {
                acWorker.Picnic();
                ConsoleHelper.WriteError("Something went wrong when trying to start the autoclicker!", exc);
            }
        }

        /// <summary>
        /// Closes the started autoclicker
        /// </summary>
        /// <param name="acWorker"></param>
        static void Picnic(AutoClickerWorker acWorker)
        {
            acWorker.Picnic();
            ConsoleHelper.WriteMessage("Autoclicker stopped!");
        }
    }
}
