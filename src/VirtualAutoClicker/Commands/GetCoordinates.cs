using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;

namespace VirtualAutoClicker.Commands
{
    
    public class GetCoordinates : ICommand
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        
        [DllImport("user32.dll")]
        
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref Win32Point pt);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct Win32Point
        {
            public int X;
            public int Y;
        };
        
        private static Point GetMousePosition()
        {
            var w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }
        
        private static void GetMousePosition(object sender, ElapsedEventArgs e)
        {
            ConsoleHelper.WriteWarning("- - - - - - - -");
            SetProcessDPIAware();
            GetWindowRect(GetForegroundWindow(), out var t2);
            
            // Cursor in screen space
            var cursor = GetMousePosition();
            
            ConsoleHelper.WriteMessage($"cursor <{cursor.X} {cursor.Y}>");
            
            ConsoleHelper.WriteMessage($"window rect location - <{t2.Location.X}> <{t2.Location.Y}>"); 
            ConsoleHelper.WriteMessage($"window rect size - <{t2.Size.Height} {t2.Size.Width}>");

            var calculatedPosition = new Win32Point()
            {
                X = cursor.X - t2.X,
                Y = cursor.Y - t2.Y,
            };
            
            ConsoleHelper.WriteMessage($"attempt to calculate pos <{calculatedPosition.X}, {calculatedPosition.Y}>");
        }

        public void Execute(string[] arguments)
        {
            if (arguments is { } && arguments.Length >= 1)
            {
                ConsoleHelper.WriteWarning("Command usage: 'getcoordinates' please refer to the readme.md file for further assistance.");
                return;
            }

            var t = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds)
            {
                AutoReset = true,
            };

            t.Elapsed += GetMousePosition;
            t.Start();
        }
    }
}
