using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MpImporter.Models;
using MpImporter.Services;
using Serilog;

namespace MpImporter.ViewModels;

public partial class ResultViewerViewModel : ObservableObject
{
    private readonly JsonResultService _jsonService;

    public string FilePath { get; }

    [ObservableProperty] private string _poNumber = string.Empty;
    [ObservableProperty] private string _oeNumber = string.Empty;
    [ObservableProperty] private string _jobNumber = string.Empty;
    [ObservableProperty] private string _lineNumber = string.Empty;
    [ObservableProperty] private string _drawingNumber = string.Empty;
    [ObservableProperty] private string _revision = string.Empty;
    [ObservableProperty] private string _drawingReleaseDate = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _deliveryRequiredDate = string.Empty;
    [ObservableProperty] private string _quantity = string.Empty;
    [ObservableProperty] private string _rawText = string.Empty;

    public ResultViewerViewModel(JsonResultService jsonService, string filePath)
    {
        _jsonService = jsonService;
        FilePath = filePath;

        var result = _jsonService.LoadTyped(filePath);
        if (result != null)
        {
            PoNumber            = result.PoNumber;
            OeNumber            = result.OeNumber;
            JobNumber           = result.JobNumber;
            LineNumber          = result.LineNumber;
            DrawingNumber       = result.DrawingNumber;
            Revision            = result.Revision;
            DrawingReleaseDate  = result.DrawingReleaseDate;
            Description         = result.Description;
            DeliveryRequiredDate = result.DeliveryRequiredDate;
            Quantity            = result.Quantity;
            RawText             = _jsonService.LoadAsText(result);
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var result = new ExtractionResult
            {
                PoNumber            = PoNumber,
                OeNumber            = OeNumber,
                JobNumber           = JobNumber,
                LineNumber          = LineNumber,
                DrawingNumber       = DrawingNumber,
                Revision            = Revision,
                DrawingReleaseDate  = DrawingReleaseDate,
                Description         = Description,
                DeliveryRequiredDate = DeliveryRequiredDate,
                Quantity            = Quantity,
            };

            foreach (var line in RawText.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                var parts = trimmed.Split('\t', 3);
                if (parts.Length < 3) continue;
                result.ProcessSteps.Add(new ProcessStep
                {
                    ShopCode           = parts[0].Trim(),
                    RowNumber          = int.TryParse(parts[1].Trim(), out var n) ? n : 0,
                    ProcessDescription = parts[2].Trim()
                });
            }

            _jsonService.SaveAll(result, FilePath);
            Log.Information("Result viewer saved changes to {FilePath}", FilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save result viewer changes");
        }
    }
}
