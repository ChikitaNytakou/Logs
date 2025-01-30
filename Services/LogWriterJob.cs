using Quartz;

public class LogWriterJob : IJob
{
    private readonly ILogger<LogWriterJob> _logger;

    public LogWriterJob(ILogger<LogWriterJob> logger)
    {
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation($"Job executed at: {DateTime.Now}");
        return Task.CompletedTask;
    }
}