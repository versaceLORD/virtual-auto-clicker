using System;

namespace virtual_autoclicker_console
{
    public static class ConsoleHelper
    {
        public static void WriteMessage(string message)
        {
            Console.WriteLine(message);
        }

        public static void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"VAC >> {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteError(string message, Exception? e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"VAC >> {message}", e);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteError(Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
