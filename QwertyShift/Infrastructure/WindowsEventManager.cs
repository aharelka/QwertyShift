using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QwertyShift
{
    /// <summary>
    /// Manages low-level Windows events, including keyboard hooks and shell messages, 
    /// to detect typing activity and potential keyboard layout changes.
    /// </summary>
    public class WindowsEventManager : IDisposable
    {
        public event Action<bool> LayoutCheckRequired; 

        public int TypingPauseMs { get; set; } = 1500;

        private IntPtr _hookID = IntPtr.Zero;
        private IntPtr _winEventHook = IntPtr.Zero;
        private ShellMessageWindow _shellWindow;

        private WinEventDelegate _winEventDelegate;
        private LowLevelKeyboardProc _keyboardProcDelegate;

        private readonly BlockingCollection<long> _keyEventsQueue = new BlockingCollection<long>();
        private readonly CancellationTokenSource _appCts = new CancellationTokenSource();

        // Stores the tick count of the last registered typing keystroke
        private long _lastKeyPressTick = 0;

        /// <summary>
        /// Initializes a new instance of the WindowsEventManager and sets up system hooks.
        /// </summary>
        public WindowsEventManager()
        {
            _winEventDelegate = new WinEventDelegate(WinEventCallback);
            _keyboardProcDelegate = new LowLevelKeyboardProc(HookCallback);

            _winEventHook = SetWinEventHook(3, 3, IntPtr.Zero, _winEventDelegate, 0, 0, 0);

            _shellWindow = new ShellMessageWindow();
            _shellWindow.LanguageChanged += () =>
            {                
                LayoutCheckRequired?.Invoke(false);
            };

            StartKeyProcessor();
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                _hookID = SetWindowsHookEx(13, _keyboardProcDelegate, GetModuleHandle(module.ModuleName), 0);
            }
        }

        /// <summary>
        /// Callback function for foreground window changes.
        /// </summary>
        private void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            LayoutCheckRequired?.Invoke(false);
        }

        /// <summary>
        /// Callback function for the low-level keyboard hook. Filters and queues typing events.
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)0x0100 || wParam == (IntPtr)0x0104))
            {
                var kbStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if (IsTypingActivity(kbStruct.vkCode))
                {
                    try { if (!_keyEventsQueue.IsAddingCompleted) _keyEventsQueue.Add((long)GetTickCount64()); }
                    catch (InvalidOperationException) { }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Starts a background task to process queued typing events and calculate typing pauses.
        /// </summary>
        private void StartKeyProcessor()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    foreach (long currentTick in _keyEventsQueue.GetConsumingEnumerable(_appCts.Token))
                    {
                        long lastTick = Interlocked.Read(ref _lastKeyPressTick);
                        bool isNewSession = (currentTick - lastTick) > TypingPauseMs;
                        Interlocked.Exchange(ref _lastKeyPressTick, currentTick);                        
                        LayoutCheckRequired?.Invoke(isNewSession);
                    }
                }
                catch (OperationCanceledException) { }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Determines whether a specific virtual key code represents actual text input.
        /// </summary>
        /// <param name="vkCode">The virtual key code to evaluate.</param>
        /// <returns>True if the key represents text input; otherwise, false.</returns>
        private static bool IsTypingActivity(uint vkCode)
        {
            Keys key = (Keys)(int)vkCode;
            bool isCtrlDown = (GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
            bool isAltDown = (GetAsyncKeyState(Keys.Menu) & 0x8000) != 0;
            bool isWinDown = (GetAsyncKeyState(Keys.LWin) & 0x8000) != 0 || (GetAsyncKeyState(Keys.RWin) & 0x8000) != 0;
            bool isAltGr = isCtrlDown && isAltDown;

            if (isWinDown || (isCtrlDown && !isAltGr) || (isAltDown && !isCtrlDown)) return false;
            if (key >= Keys.A && key <= Keys.Z) return true;
            if (key >= Keys.D0 && key <= Keys.D9) return true;
            if (key == Keys.Space || key == Keys.Back || key == Keys.Enter) return true;

            if (key == Keys.OemSemicolon || key == Keys.Oemplus || key == Keys.Oemcomma ||
                key == Keys.OemMinus || key == Keys.OemPeriod || key == Keys.OemQuestion ||
                key == Keys.Oemtilde || key == Keys.OemOpenBrackets || key == Keys.OemPipe ||
                key == Keys.OemCloseBrackets || key == Keys.OemQuotes || key == Keys.OemBackslash) return true;

            return false;
        }

        /// <summary>
        /// Unhooks system events and safely releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID);
            if (_winEventHook != IntPtr.Zero) UnhookWinEvent(_winEventHook);

            _shellWindow?.Dispose();
            _keyEventsQueue.CompleteAdding();
            _appCts.Cancel();
            _keyEventsQueue.Dispose();
            _appCts.Dispose();
        }

        /// <summary>
        /// A hidden native window used specifically to catch SHELLHOOK messages from the operating system.
        /// </summary>
        private class ShellMessageWindow : NativeWindow, IDisposable
        {
            private readonly int _shellHookMessage;
            public event Action LanguageChanged;

            public ShellMessageWindow()
            {
                var cp = new CreateParams { Caption = "QwertyShift_Hidden" };
                CreateHandle(cp);
                _shellHookMessage = RegisterWindowMessage("SHELLHOOK");
                RegisterShellHookWindow(this.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == _shellHookMessage && m.WParam.ToInt32() == 8) LanguageChanged?.Invoke();
                base.WndProc(ref m);
            }

            public void Dispose()
            {
                if (Handle != IntPtr.Zero)
                {
                    DeregisterShellHookWindow(Handle);
                    DestroyHandle();
                }
            }
            [DllImport("user32.dll", SetLastError = true)] private static extern int RegisterWindowMessage(string lpString);
            [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterShellHookWindow(IntPtr hWnd);
            [DllImport("user32.dll", SetLastError = true)] private static extern bool DeregisterShellHookWindow(IntPtr hWnd);
        }

        // P/Invoke declarations for Windows API
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll")] private static extern ulong GetTickCount64();
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(Keys vKey);
        [StructLayout(LayoutKind.Sequential)] private struct KBDLLHOOKSTRUCT { public uint vkCode; public uint scanCode; public uint flags; public uint time; public IntPtr dwExtraInfo; }
    }
}