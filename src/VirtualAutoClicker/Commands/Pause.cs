namespace VirtualAutoClicker.Commands
{
    public class Pause : ICommand
    {
        public void Execute(string[] arguments)
        {
            if (arguments.Length != 1)
            {
                ConsoleHelper.WriteError("Expected one argument, example Pause usage 'pause N'");
                return;
            }
            
            var acName = arguments[0];
            
            var acWorker = VacEnvironment.GetAcWorker();
            acWorker?.GetAutoclicker(acName)?.Pause();

            ConsoleHelper.WriteMessage($"autoclicker {acName} paused!");
        }
    }
}