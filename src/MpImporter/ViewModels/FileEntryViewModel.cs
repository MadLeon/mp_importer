using System;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MpImporter.Services;
using Serilog;

namespace MpImporter.ViewModels;

public partial class FileEntryViewModel : ObservableObject
{
    private readonly ExcelExtractorService _extractor;
    private readonly JsonResultService _jsonService;
    private readonly string _resultsDir;
    private readonly Action<FileEntryViewModel> _removeAction;

    private string? _jsonFilePath;

    public string FilePath { get; }
    public string FileName => Path.GetFileName(FilePath);
    public string? JsonFilePath => _jsonFilePath;

    [ObservableProperty] private bool _isChecked;
    [ObservableProperty] private int _index;
    [ObservableProperty] private bool _hasResult;

    public FileEntryViewModel(
        string filePath,
        ExcelExtractorService extractor,
        JsonResultService jsonService,
        string resultsDir,
        Action<FileEntryViewModel> removeAction)
    {
        FilePath = filePath;
        _extractor = extractor;
        _jsonService = jsonService;
        _resultsDir = resultsDir;
        _removeAction = removeAction;
    }

    [RelayCommand]
    private void OpenFile()
    {
        try
        {
            Process.Start(new ProcessStartInfo(FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open file {FilePath}", FilePath);
        }
    }

    [RelayCommand]
    private void Extract()
    {
        try
        {
            var result = _extractor.Extract(FilePath);
            _jsonService.Save(result, _resultsDir);
            _jsonFilePath = _jsonService.GetResultPath(_resultsDir, result.DrawingNumber);
            HasResult = File.Exists(_jsonFilePath);
            Log.Information("Extracted {FilePath} → {JsonPath}", FilePath, _jsonFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Extraction failed for {FilePath}", FilePath);
            System.Windows.MessageBox.Show(
                $"Extraction failed: {ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(HasResult))]
    private void ViewResults()
    {
        if (_jsonFilePath == null || !File.Exists(_jsonFilePath))
        {
            System.Windows.MessageBox.Show("Result file not found.", "Notice");
            return;
        }

        var vm = new ResultViewerViewModel(_jsonService, _jsonFilePath);
        var window = new Views.ResultViewerWindow { DataContext = vm };
        window.ShowDialog();
    }

    [RelayCommand]
    private void Remove() => _removeAction(this);

    partial void OnHasResultChanged(bool value)
    {
        ViewResultsCommand.NotifyCanExecuteChanged();
    }
}
