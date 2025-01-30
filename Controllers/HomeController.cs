using Logs.Services;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ILogService _logService;

    public HomeController(ILogger<HomeController> logger, ILogService logService)
    {
        _logger = logger;
        _logService = logService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetLogs()
    {
        try
        {
            var logs = _logService.ReadLogs(DateTime.Now);
            return Json(new { success = true, data = logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading logs");
            return Json(new { success = false, error = "Ошибка при чтении логов" });
        }
    }

    [HttpGet]
    public IActionResult GetLogsByDate(DateTime date)
    {
        try
        {
            var logs = _logService.ReadLogs(date);
            return Json(new { success = true, data = logs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading logs for date: {Date}", date);
            return Json(new { success = false, error = $"Ошибка при чтении логов за {date:dd.MM.yyyy}" });
        }
    }
}