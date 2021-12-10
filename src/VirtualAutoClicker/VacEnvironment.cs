using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using VirtualAutoClicker.Commands;
using VirtualAutoClicker.Enums;
using VirtualAutoClicker.Models;

namespace VirtualAutoClicker
{
    /// <summary>
    /// Virtual Autoclicker Environment, holds the initialized Autoclicker and is used as a singleton
    /// throughout the application.
    /// </summary>
    public static class VacEnvironment
    {
        public const string VersionNumber = "1.1.3";

        private static AutoClickerWorker? AutoClickerWorker;

        public static Configuration? Configuration;
        
        public static bool Active { get; private set; }

        private static readonly Dictionary<Command, ICommand> Commands = new Dictionary<Command, ICommand>();

        /// <summary>
        /// Instantiates the 'AutoClickerWorker'
        /// </summary>
        public static void Initialize()
        {
            Active = true;
            AutoClickerWorker = new AutoClickerWorker();
            
            LoadCommands();

            Configuration = GetConfiguration();
        }

        private static Configuration? GetConfiguration()
        {
            Configuration? parsedJson;
            
            using (StreamReader r = new StreamReader(@$"{Directory.GetCurrentDirectory()}\Config.json"))
            {
                string fileContent = r.ReadToEnd();
                parsedJson = JsonSerializer.Deserialize<Configuration>(fileContent, new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                    IgnoreNullValues = false,
                });
            }

            ConsoleHelper.WriteMessage(parsedJson is { } ? "Loaded configuration..." : "No configuration found...");

            return parsedJson;
        }

        public static AutoClickerWorker? GetAcWorker()
        {
            return AutoClickerWorker;
        }

        /// <summary>
        /// Loads all existing commands to the 'Commands' dictionary
        /// </summary>
        private static void LoadCommands()
        {
            Commands.TryAdd(Command.Start, new StartAutoClicker());
            Commands.TryAdd(Command.StartAutoClicker, new StartAutoClicker());
            
            Commands.TryAdd(Command.List, new ListClickers());
            Commands.TryAdd(Command.ListAll, new ListClickers());
            Commands.TryAdd(Command.ListAutoClickers, new ListClickers());
            
            Commands.TryAdd(Command.ResumeAutoClicker, new Resume());
            Commands.TryAdd(Command.Resume, new Resume());
            
            Commands.TryAdd(Command.Pause, new Pause());
            Commands.TryAdd(Command.PauseAutoClicker, new Pause());
            
            Commands.TryAdd(Command.Stop, new Picnic());
            Commands.TryAdd(Command.StopAutoClicker, new Picnic());
            Commands.TryAdd(Command.Picnic, new Picnic());

            Commands.TryAdd(Command.GetCoordinates, new GetCoordinates());
        }

        public static ICommand? GetCommand(string commandName)
        {
            Enum.TryParse<Command>(commandName, true, out var commandToExecute);
            if (commandToExecute == Command.Unknown)
            {
                return null;
            }
            
            Commands.TryGetValue(commandToExecute, out var command);
            return command;
        }

        public static void Close() => Active = false;
    }
}
