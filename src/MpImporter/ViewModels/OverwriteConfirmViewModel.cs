using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using MpImporter.Models;

namespace MpImporter.ViewModels;

public partial class OverwriteConfirmViewModel : ObservableObject
{
    public string DrawingNumber { get; }
    public string Revision { get; }
    public string ExistingText { get; }
    public string NewText { get; }

    public bool Confirmed { get; private set; }

    public OverwriteConfirmViewModel(
        string drawingNumber,
        string revision,
        IEnumerable<ProcessStep> existing,
        IEnumerable<ProcessStep> incoming)
    {
        DrawingNumber = drawingNumber;
        Revision      = revision;
        ExistingText  = FormatSteps(existing);
        NewText       = FormatSteps(incoming);
    }

    public void Overwrite()
    {
        Confirmed = true;
    }

    public void Skip()
    {
        Confirmed = false;
    }

    private static string FormatSteps(IEnumerable<ProcessStep> steps)
    {
        var sb = new StringBuilder();
        foreach (var s in steps)
            sb.AppendLine($"{s.ShopCode}\t{s.RowNumber}\t{s.ProcessDescription}");
        return sb.ToString().TrimEnd();
    }
}
