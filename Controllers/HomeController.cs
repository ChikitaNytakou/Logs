using Microsoft.AspNetCore.Mvc;
using Logs.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text;

// HomeController.cs
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly string _logPath;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _logPath = Path.Combine(Directory.GetCurrentDirectory(), "Archive", "Logs");
    }

    public IActionResult Index()
    {
        try
        {
            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }

            var folders = Directory.GetDirectories(_logPath)
                .Select(Path.GetFileName)
                .Where(folder => DateTime.TryParse(folder, out _))
                .OrderByDescending(f => f)
                .ToList();

            ViewData["Title"] = "Просмотр логов";
            return View(folders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing log directory");
            return View(new List<string>());
        }
    }

    [HttpGet]
    //[Authorize(Policy = "RequireAdministratorRole")] // Раскомментировать при необходимости
    public IActionResult OpenLogFolder(string folderName)
    {
        try
        {
            if (!DateTime.TryParse(folderName, out DateTime selectDay))
            {
                return BadRequest("Некорректная дата.");
            }

            var folderPath = Path.Combine(_logPath, selectDay.ToString("yyyy-MM-dd"));

            if (!Directory.Exists(folderPath))
            {
                return NotFound("Папка с логами не найдена.");
            }

            var stringBuilder = new StringBuilder();
            var files = Directory.GetFiles(folderPath, "*.log")
                .OrderBy(filename => filename)
                .ToList();

            foreach (var filePath in files)
            {
                try
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        stringBuilder.AppendLine($"=== {Path.GetFileName(filePath)} ===");
                        stringBuilder.AppendLine(reader.ReadToEnd());
                        stringBuilder.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error reading file: {filePath}");
                    stringBuilder.AppendLine($"Ошибка при чтении файла {Path.GetFileName(filePath)}");
                }
            }

            return Content(stringBuilder.ToString(), "text/plain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing logs");
            return BadRequest("Ошибка при открытии папки.");
        }
    }
}