using System;
using System.Linq;
using System.Text.RegularExpressions;

using VirtualAutoClicker.Enums;
using VirtualAutoClicker.Models;

namespace VirtualAutoClicker
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
                Enum.TryParse<Commands>(commandValue, true, out var command);

                switch (command)
                {
                    case Commands.Start:
                    case Commands.StartAutoClicker:
                        {
                            if (args is null || args.Length <= 2)
                            {
                                ConsoleHelper.WriteWarning("Command usage: 'startautoclicker \"P\" X,Y I N' please refer to the readme.md file for further assistance.");
                                break;
                            }

                            var proposedAcName = string.Empty;
                            if (args.Length >= 4)
                            {
                                proposedAcName = args[3];
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

                            // Instantiate the new autoclicker
                            StartAutoClicker(
                                proposedAcName,
                                processName,
                                coordinates,
                                int.Parse(args[2])
                            );

                            break;
                        }
                    case Commands.List:
                    case Commands.ListAll:
                    case Commands.ListAutoClickers:
                        {
                            ListClickers();
                            break;
                        }
                    case Commands.Stop:
                    case Commands.StopAutoClicker:
                    case Commands.Picnic:
                        {
                            Picnic();
                            break;
                        }
                    case Commands.Pause:
                    case Commands.PauseAutoClicker:
                        {
                            if (args is { } && args.Length >= 1)
                            {
                                Pause(args[0]);
                            }
                            else
                            {
                                ConsoleHelper.WriteWarning("Command usage: 'pause N' please refer to the readme.md file for further assistance.");
                                break;
                            }

                            break;
                        }
                    case Commands.Resume:
                    case Commands.ResumeAutoClicker:
                        {
                            if (args is { } && args.Length >= 1)
                            {
                                Resume(args[0]);
                            }
                            else
                            {
                                ConsoleHelper.WriteWarning("Command usage: 'resume N' please refer to the readme.md file for further assistance.");
                                break;
                            }

                            break;
                        }
                    case Commands.Unknown:
                    default:
                        {
                            ConsoleHelper.WriteWarning($"No command found named '{commandValue}'");
                            break;
                        }
                }
            }
            catch (Exception exception)
            {
                ConsoleHelper.WriteError($"Problem parsing or starting command '{commandValue}'", exception);
            }
        }

        static void StartAutoClicker(string proposedAcName, string processName, Coordinates coordinates, int interval)
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

        static void ListClickers()
        {
            var acWorker = VacEnvironment.GetAcWorker();
            var formattedString = acWorker?.GetAutoClickerStatusString();

            if (!string.IsNullOrWhiteSpace(formattedString))
            {
                ConsoleHelper.WriteMessage(formattedString);
            }
        }

        /// <summary>
        /// Closes all started autoclickers
        /// </summary>
        static void Picnic()
        {
            var acWorker = VacEnvironment.GetAcWorker();
            acWorker?.Picnic();

            ConsoleHelper.WriteMessage("Autoclickers stopped!");
        }

        private static void Pause(string acName) => GetAutoclicker(acName)?.Pause();

        private static void Resume(string acName) => GetAutoclicker(acName)?.Resume();

        private static AutoClicker? GetAutoclicker(string acName)
        {
            var acWorker = VacEnvironment.GetAcWorker();
            var ac = acWorker?.AutoClickers.FirstOrDefault(a => a.Value.Name == acName).Value;
            if (ac is null)
            {
                ConsoleHelper.WriteWarning($"Couldn't find an autoclicker named '{acName}'. Use command 'list' to see all running autoclickers.");
            }

            return ac;
        }
    }
}
