namespace VirtualAutoClicker
{
    /// <summary>
    /// Virtual Autoclicker Environment, holds the initialized Autoclicker and is used as a singleton
    /// throughout the application.
    /// </summary>
    public static class VacEnvironment
    {
        private static AutoClickerWorker? _autoClickerWorker;

        /// <summary>
        /// Instantiates the 'AutoClickerWorker'
        /// </summary>
        public static void Initialize()
        {
            _autoClickerWorker = new AutoClickerWorker();
        }

        public static AutoClickerWorker? GetAcWorker()
        {
            return _autoClickerWorker;
        }
    }
}
