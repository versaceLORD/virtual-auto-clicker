using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VirtualAutoClicker.Console.Constants;
using VirtualAutoClicker.Console.Models;

namespace VirtualAutoClicker.Console
{
    /// <summary>
    /// This class represents an virtual autoclicker instance. Holds properties and methods required
    /// to determine how and where to click in given process.
    /// </summary>
    public class AutoClicker
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// If true, the instance will click in the set process and on the set coordinates
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Entered in milliseconds
        /// </summary>
        public int Interval { get; set; }

        public Coordinates? Coordinates { get; set; }

        public string? ProcessName { get; set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        private Process? CurrentProcess { get; set; }

        public void Init()
        {
            CancellationTokenSource = new CancellationTokenSource();

            var token = CancellationTokenSource.Token;
            token.ThrowIfCancellationRequested();

            CurrentProcess = Process.GetProcessesByName(ProcessName).First();
            if (CurrentProcess?.MainWindowHandle is null)
            {
                throw new Exception($"There was no process named {ProcessName}, no autoclicker started.");
            }

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    Click();

                    await Task.Delay(Interval, token);
                }

            }, token);
        }

        /// <summary>
        /// Call this is something goes very wrong
        /// </summary>
        public void Picnic()
        {
            // No task was ever started
            if (CancellationTokenSource is null)
            {
                return;
            }

            Active = false;
            Coordinates = null;
            ProcessName = null;
            Interval = int.MaxValue;

            CancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Creates parameters which will be sent to simulate coordinates to the SendMessage message.
        /// The coordinate is relative to the upper-left corner of the client area.
        /// </summary>
        public static IntPtr CreateLParam(int lowercaseWord, int hiWord)
        {
            return (IntPtr)((hiWord << 16) | (lowercaseWord & 0xffff));
        }

        /// <summary>
        /// If all neccsary AutoClicker properties are set, send click message to the set process
        /// </summary>
        public void Click()
        {
            if (string.IsNullOrWhiteSpace(ProcessName) || !Active || Coordinates?.X is null || Coordinates?.Y is null)
            {
                return;
            }

            if (CurrentProcess is { })
            {
                SendMessage(
                    CurrentProcess.MainWindowHandle,
                    Buttons.WmLbuttondown,
                    new IntPtr(Buttons.MkLbutton),
                    CreateLParam(Coordinates.X,
                        Coordinates.Y));

                SendMessage(
                    CurrentProcess.MainWindowHandle,
                    Buttons.WmLbuttonup,
                    new IntPtr(Buttons.MkLbutton),
                    CreateLParam(Coordinates.X, Coordinates.Y));
            }
        }
    }
}
