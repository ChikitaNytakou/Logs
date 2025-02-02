using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Logs.Services
{
    public interface ILogService
    {
        string ReadLogs(DateTime date);
        List<string> GetLogFolders();
    }

    public class LogService : ILogService
    {
        private readonly ILogger<LogService> _logger;
        private readonly string _logBasePath;

        public LogService(ILogger<LogService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _logBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Archive", "Logs");
        }

        public List<string> GetLogFolders()
        {
            try
            {
                if (!Directory.Exists(_logBasePath))
                {
                    return new List<string>();
                }

                return Directory.GetDirectories(_logBasePath)
                    .Select(path => Path.GetFileName(path))
                    .Where(folder => DateTime.TryParse(folder, out _))
                    .OrderByDescending(folder => folder)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log folders");
                return new List<string>();
            }
        }

        public string ReadLogs(DateTime date)
        {
            string folderPath = Path.Combine(_logBasePath, date.ToString("yyyy-MM-dd"));

            if (!Directory.Exists(folderPath))
            {
                return string.Empty;
            }

            try
            {
                var stringBuilder = new StringBuilder();
                var logFiles = Directory.GetFiles(folderPath, "*.log")
                    .OrderBy(f => f)
                    .ToList();

                foreach (var logFile in logFiles)
                {
                    using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        stringBuilder.AppendLine($"=== {Path.GetFileName(logFile)} ===");
                        stringBuilder.AppendLine(reader.ReadToEnd());
                        stringBuilder.AppendLine();
                    }
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading logs for date: {date:yyyy-MM-dd}");
                return string.Empty;
            }
        }
    }
}