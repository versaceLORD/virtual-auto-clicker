using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace virtual_autoclicker_console
{
    public class Coordinates
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// This class represents an virtual autoclicker instance. Holds properties and methods required
    /// to determine how and where to click in given process.
    /// </summary>
    public class AutoClicker
    {
        private const uint WM_LBUTTONDOWN = 0x201;
        private const uint WM_LBUTTONUP = 0x202;
        private const uint MK_LBUTTON = 0x0001;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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

        CancellationTokenSource? CancellationTokenSource { get; set; }

        private Process? CurrentProcess { get; set; }

        public void Init()
        {
            CancellationTokenSource = new CancellationTokenSource();

            var token = CancellationTokenSource.Token;
            token.ThrowIfCancellationRequested();

            CurrentProcess = Process.GetProcessesByName(ProcessName).First();
            if (CurrentProcess == null || CurrentProcess?.MainWindowHandle == null)
            {
                throw new Exception($"There was no process named {ProcessName}, no autoclicker started.");
            }

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    Click();
                    await Task.Delay(Interval);
                }
            }, token);
        }

        /// <summary>
        /// Call this is something goes very wrong
        /// </summary>
        public void Picnic()
        {
            // No task was ever started
            if (CancellationTokenSource == null)
                return;

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
        public static IntPtr CreateLParam(int LoWord, int HiWord)
        {
            return (IntPtr)((HiWord << 16) | (LoWord & 0xffff));
        }

        /// <summary>
        /// If all neccsary AutoClicker properties are set, send click message to the set process
        /// </summary>
        public void Click()
        {
            if (string.IsNullOrWhiteSpace(ProcessName) || !Active || Coordinates?.X == null || Coordinates?.Y == null)
            {
                return;
            }

            if (CurrentProcess != null)
            {
                SendMessage(CurrentProcess.MainWindowHandle, WM_LBUTTONDOWN, new IntPtr(MK_LBUTTON), CreateLParam(Coordinates.X, Coordinates.Y));
                SendMessage(CurrentProcess.MainWindowHandle, WM_LBUTTONUP, new IntPtr(MK_LBUTTON), CreateLParam(Coordinates.X, Coordinates.Y));
            }
        }
    }
}
