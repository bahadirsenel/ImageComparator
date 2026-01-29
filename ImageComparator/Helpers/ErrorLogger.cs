using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace ImageComparator.Helpers
{
    /// <summary>
    /// Centralized error logging for diagnostics and debugging
    /// </summary>
    public static class ErrorLogger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ImageComparator",
            "Logs"
        );

        private static readonly object _logLock = new object();

        static ErrorLogger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch
            {
                // If we can't create log directory, fall back to Debug output only
            }
        }

        /// <summary>
        /// Log an error with full exception details
        /// </summary>
        /// <param name="context">Context where error occurred</param>
        /// <param name="ex">The exception</param>
        public static void LogError(string context, Exception ex)
        {
            if (ex == null) return;

            string message = FormatErrorMessage(context, ex);
            
            // Always write to debug output
            Debug.WriteLine(message);

            // Try to write to log file
            WriteToLogFile("error", message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string context, string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING - {context}: {message}";
            Debug.WriteLine(logMessage);
            WriteToLogFile("warning", logMessage);
        }

        /// <summary>
        /// Log an informational message
        /// </summary>
        public static void LogInfo(string context, string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO - {context}: {message}";
            Debug.WriteLine(logMessage);
            WriteToLogFile("info", logMessage);
        }

        /// <summary>
        /// Format exception for logging
        /// </summary>
        private static string FormatErrorMessage(string context, Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR - {context}");
            sb.AppendLine($"Exception Type: {ex.GetType().FullName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Stack Trace:\n{ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                sb.AppendLine($"\nInner Exception: {ex.InnerException.GetType().FullName}");
                sb.AppendLine($"Inner Message: {ex.InnerException.Message}");
                sb.AppendLine($"Inner Stack Trace:\n{ex.InnerException.StackTrace}");
            }
            
            sb.AppendLine(new string('-', 80));
            return sb.ToString();
        }

        /// <summary>
        /// Write to log file with thread safety
        /// </summary>
        private static void WriteToLogFile(string logType, string message)
        {
            try
            {
                lock (_logLock)
                {
                    string logFile = Path.Combine(
                        LogDirectory, 
                        $"{logType}_{DateTime.Now:yyyyMMdd}.log"
                    );
                    
                    File.AppendAllText(logFile, message + "\n");
                }
            }
            catch
            {
                // Can't log to file, but we already wrote to Debug
                // Don't throw - logging should never crash the app
            }
        }

        /// <summary>
        /// Get the path to today's error log
        /// </summary>
        public static string GetCurrentLogPath()
        {
            return Path.Combine(LogDirectory, $"error_{DateTime.Now:yyyyMMdd}.log");
        }

        /// <summary>
        /// Get the log directory path
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }

        /// <summary>
        /// Clear old log files (older than 30 days)
        /// </summary>
        public static void CleanupOldLogs()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-30);
                var files = Directory.GetFiles(LogDirectory, "*.log");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Silent failure for cleanup - not critical
            }
        }
    }
}
