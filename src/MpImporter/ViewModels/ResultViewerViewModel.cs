using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MpImporter.Services;
using Serilog;

namespace MpImporter.ViewModels;

public partial class ResultViewerViewModel : ObservableObject
{
    private readonly JsonResultService _jsonService;

    public string FilePath { get; }

    [ObservableProperty]
    private string _rawText = string.Empty;

    public ResultViewerViewModel(JsonResultService jsonService, string filePath)
    {
        _jsonService = jsonService;
        FilePath = filePath;

        var result = _jsonService.LoadTyped(filePath);
        if (result != null)
            RawText = _jsonService.LoadAsText(result);
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            _jsonService.SaveFromText(RawText, FilePath);
            Log.Information("Result viewer saved changes to {FilePath}", FilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save result viewer changes");
        }
    }
}
