using System;

namespace VirtualAutoClicker
{
    /// <summary>
    /// To keep the messages consistent throughout the application, all messages are sent through
    /// this helping class.
    /// </summary>
    public static class ConsoleHelper
    {
        private static void WriteColored(string message, ConsoleColor color)
        {
            var lastColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = lastColor;
        }

        public static void WriteMessage(string message) => WriteColored(message, ConsoleColor.Gray);
        public static void WriteWarning(string message) => WriteColored($"VAC >> {message}", ConsoleColor.Yellow);
        public static void WriteError(string message, Exception? e) => WriteColored($"VAC >> {message} \r\n {e}", ConsoleColor.Red);
        public static void WriteError(string message) => WriteColored(message, ConsoleColor.Red);
        public static void WriteError(Exception e) => WriteColored($"Error!\r\n{e}", ConsoleColor.Red);
    }
}
