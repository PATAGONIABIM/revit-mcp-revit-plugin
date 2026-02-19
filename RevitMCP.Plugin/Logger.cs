using System;
using System.IO;

namespace RevitMCP.Plugin
{
    public static class Logger
    {
        private static string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitMCP.log");

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} [{System.Threading.Thread.CurrentThread.ManagedThreadId}] {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
