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
        private static extern int SendMessage(IntPtr hWnd, uint message, int wParam, int lParam);
        
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
        /// If all neccsary AutoClicker properties are set, send click message to the set process
        /// </summary>
        private void Click()
        {
            if (string.IsNullOrWhiteSpace(ProcessName) || !Active || Coordinates is null || CurrentProcess is null)
            {
                return;
            }

            var lParam = (Coordinates.Y << 16) + Coordinates.X;
            
            SendMessage(
                CurrentProcess.MainWindowHandle,
                Buttons.WmLbuttondown,
                0,
                lParam
            );
            
            SendMessage(
                CurrentProcess.MainWindowHandle,
                Buttons.WmLbuttonup,
                0,
                lParam
            );
        }
    }
}
