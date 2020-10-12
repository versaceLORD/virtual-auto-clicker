using System;

namespace virtual_autoclicker_console
{
    /// <summary>
    /// To keep the messages consistent throughout the application, all messages are sent through
    /// this helping class.
    /// </summary>
    public static class ConsoleHelper
    {
        public static void WriteMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
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
            Console.WriteLine($"VAC >> {message} \n\r {e}");
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
            Console.WriteLine("Error!");
            Console.WriteLine(e);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
