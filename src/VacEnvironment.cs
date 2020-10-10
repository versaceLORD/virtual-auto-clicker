namespace virtual_autoclicker_console
{
    /// <summary>
    /// Virtual Autoclicker Environment, holds the initialized Autoclicker and is used as a singleton
    /// throughout the application.
    /// </summary>
    public static class VacEnvironment
    {
        private static AutoClickerWorker? AutoClickerWorker;

        /// <summary>
        /// Instantiates the 'AutoClickerWorker'
        /// </summary>
        public static void Initialize()
        {
            AutoClickerWorker = new AutoClickerWorker();
        }

        public static AutoClickerWorker? GetAcWorker()
        {
            return AutoClickerWorker;
        }
    }
}
