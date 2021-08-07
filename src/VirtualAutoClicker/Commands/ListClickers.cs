namespace VirtualAutoClicker.Commands
{
    public class ListClickers : ICommand
    {
        public void Execute(string[] arguments)
        {
            var acWorker = VacEnvironment.GetAcWorker();
            var formattedString = acWorker?.GetAutoClickerStatusString();

            if (!string.IsNullOrWhiteSpace(formattedString))
            {
                ConsoleHelper.WriteMessage(formattedString);
            }
        }
    }
}