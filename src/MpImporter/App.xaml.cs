using System.IO;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;
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

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(baseDir, "logs", "app-.log"),
                rollingInterval: Serilog.RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("MP Importer starting up");

        // Warm up ClosedXML in the background to eliminate first-extract delay
        Task.Run(() =>
        {
            using var wb = new XLWorkbook();
            wb.AddWorksheet("warmup");
            Log.Debug("ClosedXML warmup complete");
        });

        var dbPath = ResolveDbPath(baseDir);
        Log.Information("Using database: {DbPath}", dbPath);

        var resultsDir = Path.Combine(baseDir, "results");
        var extractor = new ExcelExtractorService();
        var jsonService = new JsonResultService();
        var dbService = new DatabaseService();

        var vm = new MainViewModel(extractor, jsonService, dbService, resultsDir, dbPath);
        var mainWindow = new MainWindow { DataContext = vm };
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("MP Importer shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static string ResolveDbPath(string baseDir)
    {
        // Walk up from exe directory to find data/record.db
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir.FullName, "data", "record.db");
            if (File.Exists(candidate))
                return candidate;
            if (dir.Parent == null) break;
            dir = dir.Parent;
        }

        return Path.Combine(baseDir, "data", "record.db");
    }
}
