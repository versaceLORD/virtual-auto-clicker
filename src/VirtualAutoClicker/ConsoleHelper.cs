using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualAutoClicker
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

        public static void WriteStartingMessage()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("- - - - - - - - - - - - - - - - - -");
            Console.WriteLine($"virtual-auto-clicker - Your idle success [version {VacEnvironment.VersionNumber}]");
            Console.WriteLine("by @versaceLORD - https://github.com/versaceLORD/virtual-auto-clicker");
            Console.WriteLine("- - - - - - - - - - - - - - - - - -\n\r");
        }


        /// <summary>
        /// Returns an updated array of input arguments
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns>A rebuilt list of input arguments</returns>
        public static string[] GetInputArguments(List<string> arguments)
        {
            var processNameBuilder = new StringBuilder();

            if (arguments.Count(arg => arg.Contains("\"", StringComparison.InvariantCultureIgnoreCase)) <= 0)
            {
                return arguments.ToArray();
            }

            var doubleQuoteIndex = arguments.FindIndex(arg => arg.StartsWith('"'));
            var endingIndex = -1;

            // Attempt to find the index of the ending double quote
            for (var i = 0; i < arguments.Count; i++)
            {
                if (arguments[i].EndsWith('"'))
                {
                    endingIndex = i;
                }
            }

            // We failed to find an ending double quote.
            if (endingIndex == -1)
            {
                return arguments.ToArray();
            }

            for (var i = doubleQuoteIndex; i <= endingIndex; i++)
            {
                processNameBuilder.Append($"{arguments[i]} ");
            }

            // Ensure the process name does not end with a space.
            var processName = processNameBuilder.ToString().TrimEnd();
            processName = processName.Replace("\"", "");

            arguments.RemoveRange(doubleQuoteIndex, endingIndex + 1);

            // Rebuild the arguments list with the new "merged" process name
            arguments = arguments
                .Prepend(processName)
                .ToList();

            return arguments.ToArray();
        }
    }
}
