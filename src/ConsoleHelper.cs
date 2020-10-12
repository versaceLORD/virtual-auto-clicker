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
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($"VAC >> {message}");
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteError(string message, Exception? e)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"VAC >> {message} \n\r {e}");
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteError(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteError(Exception e)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("Error!");
            System.Console.WriteLine(e);
            System.Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
