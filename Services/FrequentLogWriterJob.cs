using Quartz;
using Serilog;

public class FrequentLogWriterJob : IJob
{
    private readonly ILogger<FrequentLogWriterJob> _logger;
    private static readonly Random _random = new Random();

    private static readonly string[] _users = { "user1", "user2", "user3", "admin", "guest" };
    private static readonly string[] _actions = { "logged in", "logged out", "viewed page", "updated profile", "deleted item", "created record", "modified settings" };
    private static readonly string[] _status = { "success", "warning", "error", "info" };
    private static readonly string[] _components = { "UserService", "AuthService", "DatabaseService", "FileService", "EmailService" };
    private static readonly string[] _devices = { "Desktop", "Mobile", "Tablet" };
    private static readonly string[] _browsers = { "Chrome", "Firefox", "Safari", "Edge" };

    public FrequentLogWriterJob(ILogger<FrequentLogWriterJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        try
        {
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(GetCurrentLogPath(),
                    shared: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}",
                    fileSizeLimitBytes: null,
                    rollOnFileSizeLimit: false,
                    retainedFileCountLimit: null);

            var logger = logConfig.CreateLogger();

            int numberOfLogs = _random.Next(1, 4);

            for (int i = 0; i < numberOfLogs; i++)
            {
                var logEntry = GenerateLogEntry();
                switch (logEntry.Level)
                {
                    case "error":
                        logger.Error(logEntry.Message);
                        break;
                    case "warning":
                        logger.Warning(logEntry.Message);
                        break;
                    default:
                        logger.Information(logEntry.Message);
                        break;
                }
            }

            // Важно: закрываем логгер после использования
            logger.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating random logs");
        }

        return Task.CompletedTask;
    }

    private string GetCurrentLogPath()
    {
        string archiveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Archive");
        string logDirectory = Path.Combine(archiveDirectory, "Logs");
        string monthFolder = DateTime.Now.ToString("yyyy-MM-dd");
        string monthFolderPath = Path.Combine(logDirectory, monthFolder);

        // Создаём директории, если они не существуют
        Directory.CreateDirectory(monthFolderPath);

        // Находим текущий файл логов
        var currentLogFile = Directory.GetFiles(monthFolderPath, "log_*.log").FirstOrDefault();
        if (currentLogFile == null)
        {
            currentLogFile = Path.Combine(monthFolderPath, $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        }

        return currentLogFile;
    }

    private (string Message, string Level) GenerateLogEntry()
    {
        string user = _users[_random.Next(_users.Length)];
        string action = _actions[_random.Next(_actions.Length)];
        string status = _status[_random.Next(_status.Length)];
        string component = _components[_random.Next(_components.Length)];
        string device = _devices[_random.Next(_devices.Length)];
        string browser = _browsers[_random.Next(_browsers.Length)];
        int requestTime = _random.Next(10, 1000);

        var sessionId = Guid.NewGuid().ToString().Substring(0, 8);
        var ipAddress = $"192.168.{_random.Next(1, 255)}.{_random.Next(1, 255)}";

        string message = $"[{component}] {status.ToUpper()}: User '{user}' {action} | " +
                        $"Session: {sessionId} | IP: {ipAddress} | " +
                        $"Device: {device} | Browser: {browser} | " +
                        $"Response Time: {requestTime}ms";

        return (message, status);
    }
}