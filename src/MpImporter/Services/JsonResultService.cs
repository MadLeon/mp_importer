using System.IO;
using System.Text;
using MpImporter.Models;
using Newtonsoft.Json;
using Serilog;

namespace MpImporter.Services;

public class JsonResultService
{
    public string GetResultPath(string outputDir, string drawingNumber)
        => Path.Combine(outputDir, $"{drawingNumber}_export.json");

    public void Save(ExtractionResult result, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var path = GetResultPath(outputDir, result.DrawingNumber);
        var json = JsonConvert.SerializeObject(result, Formatting.Indented);
        File.WriteAllText(path, json, Encoding.UTF8);
        Log.Information("Saved result to {Path}", path);
    }

    public ExtractionResult? LoadTyped(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return JsonConvert.DeserializeObject<ExtractionResult>(json);
    }

    public string LoadAsText(ExtractionResult result)
    {
        var sb = new StringBuilder();
        foreach (var step in result.ProcessSteps)
            sb.AppendLine($"{step.ShopCode}\t{step.RowNumber}\t{step.ProcessDescription}");
        return sb.ToString().TrimEnd();
    }

    public void SaveAll(ExtractionResult result, string filePath)
    {
        var json = JsonConvert.SerializeObject(result, Formatting.Indented);
        File.WriteAllText(filePath, json, Encoding.UTF8);
        Log.Information("Saved result to {Path}", filePath);
    }

    public void SaveFromText(string text, string filePath)
    {
        var existing = LoadTyped(filePath);
        if (existing == null) return;

        existing.ProcessSteps.Clear();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var parts = trimmed.Split('\t', 3);
            if (parts.Length < 3) continue;
            existing.ProcessSteps.Add(new Models.ProcessStep
            {
                ShopCode = parts[0].Trim(),
                RowNumber = int.TryParse(parts[1].Trim(), out var n) ? n : 0,
                ProcessDescription = parts[2].Trim()
            });
        }

        var json = JsonConvert.SerializeObject(existing, Formatting.Indented);
        File.WriteAllText(filePath, json, Encoding.UTF8);
        Log.Information("Saved edited result to {Path}", filePath);
    }
}
