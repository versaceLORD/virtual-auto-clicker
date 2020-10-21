using System;
using System.Linq;

namespace VirtualAutoClicker
{
    /// <summary>
    /// To keep the messages consistent throughout the application, all messages are sent through
    /// this helping class.
    /// </summary>
    public static class ConsoleHelper
    {
        private static readonly object ConsoleLock = new object();

        public static void WriteMessage(string message) => WriteColored(message, ConsoleColor.Gray);
        public static void WriteWarning(string message) => WriteColored(message, ConsoleColor.Yellow);
        public static void WriteError(string message, Exception? e) => WriteColored($"{message}\r\n{e}", ConsoleColor.Red);
        public static void WriteError(string message) => WriteColored(message, ConsoleColor.Red);
        public static void WriteError(Exception e) => WriteColored($"Error!\r\n{e}", ConsoleColor.Red);

        private static void WriteColored(string message, ConsoleColor color)
        {
            lock (ConsoleLock)
            {
                var (cursorleft, cursortop, linecount) = (Console.CursorLeft, Console.CursorTop, MessageLinesInConsole(message));
                Console.MoveBufferArea(0, Console.CursorTop, cursorleft, 1, 0, cursortop + linecount);
                Console.CursorLeft = 0;

                var lastColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ForegroundColor = lastColor;

                Console.CursorLeft = cursorleft;
            }
        }

        private static int MessageLinesInConsole(string message) =>
            message.Split('\n').Aggregate(0, (a, x) => a + 1 + x.Length / Console.BufferWidth);
    }
}
