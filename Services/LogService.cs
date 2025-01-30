using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Logs.Services
{
    public interface ILogService
    {
        void WriteLog(string message);
        IEnumerable<string> ReadLogs(DateTime date);
    }

    public class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;

        public LogService(ILogger<LogService> logger)
        {
            _logger = logger;
        }

        public void WriteLog(string message)
        {
            _logger.LogInformation(message);
        }

        public IEnumerable<string> ReadLogs(DateTime date)
        {
            string monthFolder = date.ToString("yyyy-MM-dd");
            string logPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Archive",
                "Logs",
                monthFolder);

            if (!Directory.Exists(logPath))
            {
                return Enumerable.Empty<string>();
            }

            // Находим единственный файл логов
            var logFile = Directory.GetFiles(logPath, "log_*.log").FirstOrDefault();
            if (logFile == null)
            {
                return Enumerable.Empty<string>();
            }

            try
            {
                using (FileStream fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fs))
                {
                    return reader.ReadToEnd()
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"Error reading log file: {logFile}");
                return Enumerable.Empty<string>();
            }
        }
    }
}