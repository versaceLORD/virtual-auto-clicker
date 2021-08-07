namespace VirtualAutoClicker.Commands
{
    public class Picnic : ICommand
    {
        public void Execute(string[] arguments)
        {
            var acWorker = VacEnvironment.GetAcWorker();
            acWorker?.Picnic();

            ConsoleHelper.WriteMessage("All autoclickers stopped!");
        }
    }
}