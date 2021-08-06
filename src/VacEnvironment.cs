using System;
using System.Collections.Generic;

using VirtualAutoClicker.Commands;
using VirtualAutoClicker.Enums;

namespace VirtualAutoClicker
{
    /// <summary>
    /// Virtual Autoclicker Environment, holds the initialized Autoclicker and is used as a singleton
    /// throughout the application.
    /// </summary>
    public static class VacEnvironment
    {
        private static AutoClickerWorker? AutoClickerWorker;
        
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
