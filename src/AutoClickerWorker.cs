namespace virtual_autoclicker_console
{
    public class AutoClickerWorker
    {
        public AutoClicker? AutoClicker { get; set; }

        /// <summary>
        /// Stops and nulls worker's autoclicker if active
        /// </summary>
        public void Picnic()
        {
            if (AutoClicker == null)
            {
                return;
            }

            AutoClicker.Picnic();
            AutoClicker = null;
        }
    }
}
