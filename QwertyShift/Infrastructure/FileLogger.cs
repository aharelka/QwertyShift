using System;
using System.IO;

namespace QwertyShift
{
    /// <summary>
    /// Provides a thread-safe logging mechanism that writes error details to a local text file.
    /// </summary>
    public class FileLogger : ILogger
    {
        private static readonly object _logLock = new object();

        /// <summary>
        /// Records an exception and its context to the application's log file.
        /// </summary>        
        public void LogError(Exception ex, string context)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{context}]\r\n{ex?.GetType().Name}: {ex?.Message}\r\n{ex?.StackTrace}\r\n{new string('-', 40)}\r\n";
            lock (_logLock)
            {
                try
                {
                    string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.AppName);
                    Directory.CreateDirectory(appDataFolder);
                    File.AppendAllText(Path.Combine(appDataFolder, AppConstants.LogFileName), logMessage);
                }
                catch { }
            }
        }
    }
}