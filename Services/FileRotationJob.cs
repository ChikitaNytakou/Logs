using Quartz;
using Serilog;

public class FileRotationJob : IJob
{
    private readonly ILogger<FileRotationJob> _logger;
    private readonly IConfiguration _configuration;

    public FileRotationJob(ILogger<FileRotationJob> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task Execute(IJobExecutionContext context)
    {
        try
        {
            string archiveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Archive");
            string logDirectory = Path.Combine(archiveDirectory, "Logs");
            string monthFolder = DateTime.Now.ToString("yyyy-MM-dd");
            string monthFolderPath = Path.Combine(logDirectory, monthFolder);

            // Find existing log file
            var existingLogFile = Directory.GetFiles(monthFolderPath, "log_*.log").FirstOrDefault();
            if (existingLogFile != null)
            {
                string content;
                // Read with sharing
                using (var fileStream = new FileStream(existingLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream))
                {
                    content = reader.ReadToEnd();
                }

                // Create new filename
                string newLogFileName = $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                string newLogPath = Path.Combine(monthFolderPath, newLogFileName);

                // Write with sharing
                using (var fileStream = new FileStream(newLogPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(fileStream))
                {
                    writer.Write(content);
                }

                // Close existing logger
                Log.CloseAndFlush();

                // Try to delete old file with retries
                int retryCount = 0;
                while (retryCount < 3)
                {
                    try
                    {
                        if (File.Exists(existingLogFile))
                        {
                            File.Delete(existingLogFile);
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        retryCount++;
                        Thread.Sleep(100); // Wait briefly before retry
                    }
                }

                // Reconfigure logger to use new file
                var logConfig = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}\n")
                    .WriteTo.File(newLogPath,
                        shared: true,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}",
                        fileSizeLimitBytes: null,
                        rollOnFileSizeLimit: false,
                        retainedFileCountLimit: null);

                Log.Logger = logConfig.CreateLogger();

                _logger.LogInformation($"Log file rotated to: {newLogFileName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file rotation");
        }

        return Task.CompletedTask;
    }
}