using System.Threading.Tasks;
using MpImporter.Models;
using Serilog;

namespace MpImporter.Services;

public class DatabaseService
{
    public Task UploadAsync(ExtractionResult result, string dbPath)
    {
        Log.Warning("DatabaseService.UploadAsync not yet implemented (Phase 3).");
        throw new NotImplementedException("Database upload will be implemented in Phase 3.");
    }
}
