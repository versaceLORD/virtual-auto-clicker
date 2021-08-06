namespace VirtualAutoClicker.Commands
{
    public class Resume : ICommand
    {
        public void Execute(string[] arguments)
        {
            if (arguments.Length != 1)
            {
                ConsoleHelper.WriteError("Expected one argument, example Resume usage 'resume N'");
                return;
            }
            
            var acName = arguments[0];
            
            var acWorker = VacEnvironment.GetAcWorker();
            acWorker?.GetAutoclicker(acName)?.Resume();

            ConsoleHelper.WriteMessage($"autoclicker {acName} resumed!");
        }
    }
}