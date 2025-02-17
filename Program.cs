using Quartz;
using Serilog;
using Logs.Services;
using Serilog.Events;
using Serilog.Sinks.File;
using Logs.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure logging paths
string archiveDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Archive");
string logDirectory = Path.Combine(archiveDirectory, "Logs");
string monthFolder = DateTime.Now.ToString("yyyy-MM-dd");
string monthFolderPath = Path.Combine(logDirectory, monthFolder);

// Create necessary directories
Directory.CreateDirectory(archiveDirectory);
Directory.CreateDirectory(logDirectory);
Directory.CreateDirectory(monthFolderPath);

// Handle log file
string newLogFileName = $"log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
string currentLogFile = Path.Combine(monthFolderPath, newLogFileName);

// Найти и переименовать существующий файл логов
var existingLogFile = Directory.GetFiles(monthFolderPath, "log_*.log").FirstOrDefault();
if (existingLogFile != null)
{
    // Копируем содержимое старого файла
    string content = File.ReadAllText(existingLogFile);
    // Удаляем старый файл
    File.Delete(existingLogFile);
    // Создаем новый файл с новым именем и старым содержимым
    File.WriteAllText(currentLogFile, content);
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}\n")
    .WriteTo.File(currentLogFile,
        shared: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}",
        fileSizeLimitBytes: null,
        rollOnFileSizeLimit: false,
        retainedFileCountLimit: null,
        buffered: false,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ILogService, LogService>();

// Add Session configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Quartz
builder.Services.AddQuartz(q =>
{
    // Настройка задачи для частого логирования (каждые 15 секунд)
    var frequentLogJobKey = new JobKey("FrequentLogWriterJob");
    q.AddJob<FrequentLogWriterJob>(opts => opts.WithIdentity(frequentLogJobKey));
    q.AddTrigger(opts => opts
        .ForJob(frequentLogJobKey)
        .WithIdentity("FrequentLogWriterTrigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(15)
            .RepeatForever()));

    // Настройка задачи для ротации файлов (каждые 30 секунд)
    var fileRotationJobKey = new JobKey("FileRotationJob");
    q.AddJob<FileRotationJob>(opts => opts.WithIdentity(fileRotationJobKey));
    q.AddTrigger(opts => opts
        .ForJob(fileRotationJobKey)
        .WithIdentity("FileRotationTrigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(30)
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Middleware для установки версии в HttpContext
app.Use(async (context, next) =>
{
    context.Items["VersionInfo"] = VersionInfo.Version; // Сохраните версию
    await next.Invoke();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();