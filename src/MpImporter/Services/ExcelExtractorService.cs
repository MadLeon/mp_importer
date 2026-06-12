using ClosedXML.Excel;
using MpImporter.Models;
using Serilog;

namespace MpImporter.Services;

public class ExcelExtractorService
{
    private const int ProcessRowStart = 11;
    private const int ProcessRowEnd = 26;
    private const int ColShopCode = 4;        // D
    private const int ColRowNumber = 5;       // E
    private const int ColDescription = 6;    // F (master cell of F:N merge)

    public ExtractionResult Extract(string filePath)
    {
        Log.Information("Extracting {FilePath}", filePath);

        using var workbook = new XLWorkbook(filePath);
        var ws = workbook.Worksheets.First();

        var result = new ExtractionResult
        {
            PoNumber            = GetString(ws, "B7"),
            OeNumber            = GetString(ws, "N6"),
            JobNumber           = GetString(ws, "Q6"),
            LineNumber          = GetString(ws, "F7"),
            DrawingNumber       = GetString(ws, "J7"),
            Revision            = GetString(ws, "R7"),
            DrawingReleaseDate  = GetString(ws, "B8"),
            Description         = GetString(ws, "H8"),
            DeliveryRequiredDate = GetString(ws, "D9"),
            Quantity            = GetString(ws, "Q9"),
        };

        for (int row = ProcessRowStart; row <= ProcessRowEnd; row++)
        {
            var shopCode    = GetString(ws, row, ColShopCode);
            var description = GetString(ws, row, ColDescription);

            if (string.IsNullOrWhiteSpace(shopCode) && string.IsNullOrWhiteSpace(description))
                continue;

            if (!int.TryParse(GetString(ws, row, ColRowNumber), out var rowNumber))
                rowNumber = (row - ProcessRowStart + 1) * 10;

            result.ProcessSteps.Add(new ProcessStep
            {
                ShopCode           = shopCode,
                RowNumber          = rowNumber,
                ProcessDescription = description,
            });
        }

        Log.Information(
            "Extracted {Drawing} rev {Rev} — {Steps} process step(s)",
            result.DrawingNumber, result.Revision, result.ProcessSteps.Count);

        return result;
    }

    private static string GetString(IXLWorksheet ws, string address)
        => ws.Cell(address).GetString().Trim();

    private static string GetString(IXLWorksheet ws, int row, int col)
        => ws.Cell(row, col).GetString().Trim();
}
