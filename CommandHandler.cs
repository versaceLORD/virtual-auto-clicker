using System;

namespace virtual_autoclicker_console
{
    public static class CommandHandler
    {
        public static void ParseCommand(string command, string[]? args)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            var acWorker = VacEnvironment.GetAcWorker();
            if (acWorker == null)
            {
                ConsoleHelper.WriteError("AutoClickerWorker was not properly initialized, please restart the application.");
                return;
            }

            try
            {
                switch (command.ToLower())
                {
                    case "startautoclicker":
                        {
                            if (args != null && args.Length <= 2)
                            {
                                Console.WriteLine("Please provide at least one argument");
                                break;
                            }

                            var coordinates = new Coordinates
                            {
                                X = int.Parse(args[1].Split(',')[0]),
                                Y = int.Parse(args[1].Split(',')[1]),
                            };

                            StartAutoClicker(acWorker, args[0], coordinates, int.Parse(args[2]));

                            break;
                        }
                    case "stopautoclicker":
                    case "picnic":
                    case "stop":
                        {
                            Picnic(acWorker);
                            break;
                        }
                    default:
                        ConsoleHelper.WriteWarning($"No command found named '{command.ToLower()}'");
                        break;
                }


            }
            catch (Exception exc)
            {
                ConsoleHelper.WriteError($"Problem parsing or starting command '{command}'", exc);
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
                acWorker.AutoClicker = ac;
                ac.Init();
            }
            catch (Exception exc)
            {
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
        }
    }
}
