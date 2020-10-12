using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace virtual_autoclicker_console
{
    /// <summary>
    /// Written in lower case since all commands are entered in lowercase.
    /// </summary>
    public enum Commands
    {
        Unknown,

        List,
        ListAutoClickers,
        ListAll,
        ShowAll,

        StartAutoClicker,
        Start,

        Stop,
        StopAutoClicker,
        Picnic,

        Pause,
        PauseAutoClicker,

        Resume,
        ResumeAutoClicker,
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
                Enum.TryParse<Commands>(commandStr, false, out var command);
                switch (command)
                {
                    case Commands.Start:
                    case Commands.StartAutoClicker:
                        {
                            if (args == null || args.Length < 3)
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
                            if (args != null && args.Length >= 1)
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
                            if (args != null && args.Length >= 1)
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
                        ConsoleHelper.WriteWarning($"No command found named '{commandStr}'");
                        break;
                }


            }
            catch (Exception exc)
            {
                ConsoleHelper.WriteError($"Problem parsing or starting command '{commandStr}'", exc);
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
                if (acWorker != null && acName != null)
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

        static void Pause(string acName)
        {
            var acWorker = VacEnvironment.GetAcWorker();
            try
            {
                var ac = acWorker?.AutoClickers.First(a => a.Value.Name == acName).Value;
                if (ac == null)
                {
                    throw new Exception();
                }

                ac.Pause();
            }
            catch (Exception)
            {
                ConsoleHelper.WriteWarning($"Couldn't find any autoclicker '{acName}', please check for any spelling errors, or use command 'list' to see all running autoclickers.");
                return;
            }
        }

        static void Resume(string acName)
        {
            var acWorker = VacEnvironment.GetAcWorker();
            try
            {
                var ac = acWorker?.AutoClickers.First(a => a.Value.Name == acName).Value;
                if (ac == null)
                {
                    throw new Exception();
                }

                ac.Resume();
            }
            catch (Exception)
            {
                ConsoleHelper.WriteWarning($"Couldn't find any autoclicker '{acName}', please check for any spelling errors, or use command 'list' to see all running autoclickers.");
                return;
            }
        }
    }
}
