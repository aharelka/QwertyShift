using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QwertyShift
{
    /// <summary>
    /// Detects active keyboard layout changes in the foreground window and retrieves layout names.
    /// </summary>
    public class LayoutDetector
    {
        public event Action<KeyboardLayoutInfo> LayoutChanged;

        private IntPtr _currentLayout = IntPtr.Zero;
        private readonly object _stateLock = new object();
        private readonly ConcurrentDictionary<IntPtr, string> _layoutNamesCache = new ConcurrentDictionary<IntPtr, string>();
        private readonly ConcurrentDictionary<IntPtr, string> _customNames = new ConcurrentDictionary<IntPtr, string>();

        /// <summary>
        /// Normalizes the layout pointer for 64-bit operating systems by clearing the upper bits.
        /// </summary>
        private static IntPtr NormalizeHKL(IntPtr hkl)
        {
            return (IntPtr)(hkl.ToInt64() & 0xFFFFFFFF);
        }

        /// <summary>
        /// Assigns a custom user-defined name to a specific keyboard layout.
        /// </summary>
        public void SetCustomName(IntPtr hkl, string customName)
        {
            hkl = NormalizeHKL(hkl); 
            _customNames[hkl] = customName;
        }

        /// <summary>
        /// Retrieves comprehensive information about a keyboard layout, including system and custom names.
        /// </summary>
        /// <param name="hkl">The handle of the keyboard layout.</param>
        public KeyboardLayoutInfo GetLayoutInfo(IntPtr hkl)
        {
            hkl = NormalizeHKL(hkl); 

            if (!_layoutNamesCache.TryGetValue(hkl, out string layoutName))
            {
                layoutName = ResolveLayoutNameFromRegistry(hkl);
                _layoutNamesCache[hkl] = layoutName;
            }

            _customNames.TryGetValue(hkl, out string customName);
            return new KeyboardLayoutInfo(hkl, layoutName, customName);
        }

        /// <summary>
        /// Asynchronously checks the current active layout and triggers the LayoutChanged event if a change is detected.
        /// </summary>
        public void TriggerCheck(bool isNewTypingSession)
        {
            Task.Run(() =>
            {
                try
                {
                    IntPtr actualLayout = GetCurrentLayoutHKL();
                    if (actualLayout == IntPtr.Zero) return;

                    actualLayout = NormalizeHKL(actualLayout); 

                    bool layoutChanged = false;
                    lock (_stateLock)
                    {
                        if (actualLayout != _currentLayout)
                        {
                            _currentLayout = actualLayout;
                            layoutChanged = true;
                        }
                    }

                    if (layoutChanged || isNewTypingSession)
                    {
                        var info = GetLayoutInfo(actualLayout);
                        LayoutChanged?.Invoke(info);
                    }
                }
                catch { }
            });
        }

        /// <summary>
        /// Resolves the human-readable name of the layout by querying the Windows Registry.
        /// </summary>
        private static string ResolveLayoutNameFromRegistry(IntPtr hkl)
        {
            try
            {
                uint hklValue = (uint)(hkl.ToInt64() & 0xFFFFFFFF);
                string klid = hklValue.ToString("X8");
                if ((hklValue >> 16) == (hklValue & 0xFFFF)) klid = "0000" + (hklValue & 0xFFFF).ToString("X4");

                using (RegistryKey subKey = Registry.CurrentUser.OpenSubKey(@"Keyboard Layout\Substitutes"))
                {
                    string substitute = subKey?.GetValue(klid) as string;
                    if (!string.IsNullOrEmpty(substitute)) klid = substitute;
                }

                using (RegistryKey layoutKey = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\{klid}"))
                {
                    string layoutName = layoutKey?.GetValue("Layout Text") as string;
                    if (!string.IsNullOrEmpty(layoutName)) return layoutName;
                }
            }
            catch { }
            return "Unknown keyboard";
        }

        /// <summary>
        /// Retrieves the keyboard layout handle for the currently active (foreground) window.
        /// </summary>
        private static IntPtr GetCurrentLayoutHKL()
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return IntPtr.Zero;

            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            GUITHREADINFO guiInfo = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf(typeof(GUITHREADINFO)) };

            if (GetGUIThreadInfo(foregroundThreadId, ref guiInfo))
            {
                IntPtr targetWindow = guiInfo.hwndFocus != IntPtr.Zero ? guiInfo.hwndFocus : guiInfo.hwndActive;
                if (targetWindow != IntPtr.Zero) return GetKeyboardLayout(GetWindowThreadProcessId(targetWindow, out _));
            }
            return GetKeyboardLayout(foregroundThreadId);
        }

        // P/Invoke declarations for Windows API calls
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint idThread);
        [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);
        [StructLayout(LayoutKind.Sequential)] private struct GUITHREADINFO { public uint cbSize; public uint flags; public IntPtr hwndActive; public IntPtr hwndFocus; public IntPtr hwndCapture; public IntPtr hwndMenuOwner; public IntPtr hwndMoveSize; public IntPtr hwndCaret; public RECT rcCaret; }
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int left; public int top; public int right; public int bottom; }
    }
}