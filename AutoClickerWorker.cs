namespace virtual_autoclicker_console
{
    public class AutoClickerWorker
    {
        public AutoClicker? AutoClicker { get; set; }

        public void Picnic()
        {
            if (AutoClicker == null)
            {
                return;
            }

            AutoClicker.Picnic();
        }
    }
}
