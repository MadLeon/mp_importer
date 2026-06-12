using System.IO;
using System.Windows;
using MpImporter.Services;
using MpImporter.ViewModels;
using Serilog;

namespace MpImporter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var logsDir = Path.Combine(baseDir, "logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logsDir, "app-.log"),
                rollingInterval: Serilog.RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("MP Importer starting up");

        var resultsDir = Path.Combine(baseDir, "results");
        var extractor = new ExcelExtractorService();
        var jsonService = new JsonResultService();
        var dbService = new DatabaseService();

        var vm = new MainViewModel(extractor, jsonService, dbService, resultsDir);
        var mainWindow = new MainWindow { DataContext = vm };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("MP Importer shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
