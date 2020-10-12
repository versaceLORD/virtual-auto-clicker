namespace VirtualAutoClicker.Console
{
    public class AutoClickerWorker
    {
        public AutoClicker? AutoClicker { get; set; }

        /// <summary>
        /// Stops and nulls worker's autoclicker if active
        /// </summary>
        public void Picnic()
        {
            if (AutoClicker is null)
            {
                return;
            }

            AutoClicker.Picnic();

            AutoClicker = null;
        }
    }
}
