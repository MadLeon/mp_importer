using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MpImporter.Services;
using Serilog;

namespace MpImporter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ExcelExtractorService _extractor;
    private readonly JsonResultService _jsonService;
    private readonly DatabaseService _dbService;
    private readonly string _resultsDir;

    public ObservableCollection<FileEntryViewModel> FileEntries { get; } = new();

    public MainViewModel(
        ExcelExtractorService extractor,
        JsonResultService jsonService,
        DatabaseService dbService,
        string resultsDir)
    {
        _extractor = extractor;
        _jsonService = jsonService;
        _dbService = dbService;
        _resultsDir = resultsDir;
    }

    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Excel Files|*.xlsm;*.xlsx",
            Title = "Select Manufacturing Process Files"
        };
        if (dialog.ShowDialog() != true) return;

        AddFilesByPaths(dialog.FileNames);
    }

    public void AddFilesByPaths(IEnumerable<string> paths)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xlsm", ".xlsx" };
        var newFiles = paths
            .Where(f => allowed.Contains(Path.GetExtension(f)))
            .Where(f => FileEntries.All(e => e.FilePath != f))
            .OrderBy(Path.GetFileName);

        int added = 0;
        foreach (var f in newFiles)
        {
            FileEntries.Add(new FileEntryViewModel(f, _extractor, _jsonService, _resultsDir, RemoveEntry));
            added++;
        }

        if (added > 0)
        {
            Reindex();
            Log.Information("Added {Added} file(s), total {Total}", added, FileEntries.Count);
        }
    }

    [RelayCommand]
    private void BatchExtract()
    {
        var targets = FileEntries.Where(e => e.IsChecked).ToList();
        foreach (var entry in targets)
            entry.ExtractCommand.Execute(null);
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        var targets = FileEntries.Where(e => e.IsChecked).ToList();
        foreach (var entry in targets)
            FileEntries.Remove(entry);
        Reindex();
        Log.Information("Removed {Count} selected file(s)", targets.Count);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task UploadDataAsync()
    {
        var targets = FileEntries.Where(e => e.IsChecked && e.HasResult).ToList();
        if (targets.Count == 0)
        {
            System.Windows.MessageBox.Show("No checked entries with extraction results.", "Notice");
            return;
        }

        var confirm = System.Windows.MessageBox.Show(
            $"Upload {targets.Count} record(s) to the database?",
            "Confirm Upload",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        int success = 0, failed = 0;
        foreach (var entry in targets)
        {
            try
            {
                // TODO Phase 3: implement DatabaseService and pass correct dbPath
                await _dbService.UploadAsync(null!, string.Empty);
                success++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Upload failed for {FileName}", entry.FileName);
                failed++;
            }
        }

        System.Windows.MessageBox.Show(
            $"Upload complete: {success} succeeded, {failed} failed.",
            "Upload Result",
            System.Windows.MessageBoxButton.OK,
            failed > 0 ? System.Windows.MessageBoxImage.Warning : System.Windows.MessageBoxImage.Information);
    }

    private void RemoveEntry(FileEntryViewModel entry)
    {
        FileEntries.Remove(entry);
        Reindex();
        Log.Information("Removed file {FileName}", entry.FileName);
    }

    private void Reindex()
    {
        for (int i = 0; i < FileEntries.Count; i++)
            FileEntries[i].Index = i + 1;
    }
}
