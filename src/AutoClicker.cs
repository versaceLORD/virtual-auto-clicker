using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using VirtualAutoClicker.Constants;
using VirtualAutoClicker.Models;

namespace VirtualAutoClicker
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
        /// Name of instance
        /// </summary>
        public string? Name { get; set; }

        public string? ProcessName { get; set; }

        /// <summary>
        /// If true, the instance will click in the set process and on the set coordinates
        /// </summary>
        public bool Active { get; set; }

        public Coordinates? Coordinates { get; set; }

        /// <summary>
        /// Entered in milliseconds
        /// </summary>
        public int Interval { get; set; }

        private CancellationTokenSource? CancellationTokenSource { get; set; }

        private Process? CurrentProcess { get; set; }

        public void Init()
        {
            CancellationTokenSource = new CancellationTokenSource();

            CurrentProcess = Process.GetProcessesByName(ProcessName).FirstOrDefault();
            if (CurrentProcess?.MainWindowHandle is null)
            {
                throw new Exception($"There was no process named {ProcessName}, no autoclicker started.");
            }

            StartClicker();
        }

        /// <summary>
        /// Starts clicker task
        /// </summary>
        private void StartClicker()
        {
            if (CancellationTokenSource is null)
            {
                ConsoleHelper.WriteError("Tried to start a autoclicker's clicking task without a token source");
                return;
            }

            var token = CancellationTokenSource.Token;
            token.ThrowIfCancellationRequested();

            Task.Factory.StartNew(async (_) =>
            {
                while (!token.IsCancellationRequested)
                {
                    Click();
                    await Task.Delay(Interval, token);
                }
            }, null, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Call this is something goes very wrong, or if it's time to end this instance's life (RIP)
        /// </summary>
        public void Picnic()
        {
            // No task was ever started
            if (CancellationTokenSource is null)
            {
                return;
            }

            CancellationTokenSource.Cancel();

            Active = false;
            Interval = int.MaxValue;
            Coordinates = null;
            ProcessName = null;
        }

        /// <summary>
        /// Sets active to false and stops the current running task (if running)
        /// </summary>
        public void Pause()
        {
            Active = false;
            CancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Renews 'CancellationTokenSource' and starts clicker task
        /// </summary>
        public void Resume()
        {
            if (Active)
            {
                ConsoleHelper.WriteMessage($"Autoclicker '{Name}' is already running!");
                return;
            }
            
            Active = true;
            CancellationTokenSource = new CancellationTokenSource();

            StartClicker();
        }

        /// <summary>
        /// Creates parameters which will be sent to simulate coordinates to the SendMessage message.
        /// The coordinate is relative to the upper-left corner of the client area.
        /// </summary>
        private static IntPtr CreateLParam(int loWord, int hiWord)
        {
            return (IntPtr)((hiWord << 16) | (loWord & 0xffff));
        }

        /// <summary>
        /// If all neccsary AutoClicker properties are set, send click message to the set process
        /// </summary>
        private void Click()
        {
            if (string.IsNullOrWhiteSpace(ProcessName) || !Active || Coordinates is null || CurrentProcess is null)
            {
                return;
            }

            SendMessage(
                CurrentProcess.MainWindowHandle,
                Buttons.WmLbuttondown,
                new IntPtr(Buttons.MkLbutton),
                CreateLParam(Coordinates.X, Coordinates.Y)
            );

            SendMessage(
                CurrentProcess.MainWindowHandle,
                Buttons.WmLbuttonup,
                new IntPtr(Buttons.MkLbutton),
                CreateLParam(Coordinates.X, Coordinates.Y)
            );
        }
    }
}
