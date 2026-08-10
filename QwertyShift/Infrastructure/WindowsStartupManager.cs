using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace QwertyShift
{
    /// <summary>
    /// Manages the application's automatic startup configuration within Windows.
    /// </summary>
    public class WindowsStartupManager : IStartupManager
    {
        private readonly ILogger _logger;
        public WindowsStartupManager(ILogger logger) => _logger = logger;
    
        /// <summary>
        /// Checks if the application is currently configured to run automatically when Windows starts.
        /// </summary>
        public bool IsAutorunEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeys.AutoRunPath, false))
                    return key != null && key.GetValue(AppConstants.AppName) != null;
            }
            catch (Exception ex) { _logger.LogError(ex, "Read autorun"); return false; }
        }

        /// <summary>
        /// Enables or disables the application's automatic startup behavior.
        /// </summary>
        public bool SetAutorun(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeys.AutoRunPath, true))
                {
                    if (key == null) return false;

                    if (enable) key.SetValue(AppConstants.AppName, $"\"{Application.ExecutablePath}\" {AppConstants.AutorunSwitch}");
                    else key.DeleteValue(AppConstants.AppName, false);
                    return true;
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "Change autorun"); return false; }
        }
    }
}