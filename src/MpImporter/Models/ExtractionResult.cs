using System.Collections.Generic;

namespace MpImporter.Models;

public class ExtractionResult
{
    public string PoNumber { get; set; } = string.Empty;
    public string OeNumber { get; set; } = string.Empty;
    public string JobNumber { get; set; } = string.Empty;
    public string LineNumber { get; set; } = string.Empty;
    public string DrawingNumber { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string DrawingReleaseDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DeliveryRequiredDate { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;

    public List<ProcessStep> ProcessSteps { get; set; } = new();
}
