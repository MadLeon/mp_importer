using Microsoft.Data.Sqlite;
using MpImporter.Models;
using Serilog;

namespace MpImporter.Services;

public class DatabaseService
{
    public async Task<List<ProcessStep>?> GetExistingStepsAsync(
        ExtractionResult result, string dbPath)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();

        var partId = await FindPartIdAsync(conn, result.DrawingNumber, result.Revision);
        if (partId == null) return null;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT shop_code, row_number, description
            FROM process_template
            WHERE part_id = $pid
            ORDER BY row_number
            """;
        cmd.Parameters.AddWithValue("$pid", partId.Value);

        var steps = new List<ProcessStep>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            steps.Add(new ProcessStep
            {
                ShopCode           = reader.GetString(0),
                RowNumber          = reader.GetInt32(1),
                ProcessDescription = reader.GetString(2),
            });
        }

        return steps.Count > 0 ? steps : null;
    }

    public async Task UploadAsync(ExtractionResult result, string dbPath)
    {
        Log.Information("Uploading {Drawing} rev {Rev} to {DbPath}",
            result.DrawingNumber, result.Revision, dbPath);

        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();

        await using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync();
        }

        var partId = await FindPartIdAsync(conn, result.DrawingNumber, result.Revision);
        if (partId == null)
            throw new InvalidOperationException(
                $"Part not found in database: drawing '{result.DrawingNumber}' rev '{result.Revision}'. " +
                "Please ensure the part exists before uploading process templates.");

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            await DeleteExistingStepsAsync(conn, tx, partId.Value);
            await InsertStepsAsync(conn, tx, partId.Value, result.ProcessSteps);
            await tx.CommitAsync();

            Log.Information("Uploaded {Count} process step(s) for part_id={PartId}",
                result.ProcessSteps.Count, partId.Value);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static async Task<long?> FindPartIdAsync(
        SqliteConnection conn, string drawingNumber, string revision)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id FROM part
            WHERE drawing_number = $dn AND revision = $rev
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$dn", drawingNumber);
        cmd.Parameters.AddWithValue("$rev", string.IsNullOrWhiteSpace(revision) ? "-" : revision);

        var result = await cmd.ExecuteScalarAsync();
        return result is long id ? id : null;
    }

    private static async Task DeleteExistingStepsAsync(
        SqliteConnection conn, System.Data.Common.DbTransaction tx, long partId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = (SqliteTransaction)tx;
        cmd.CommandText = "DELETE FROM process_template WHERE part_id = $pid";
        cmd.Parameters.AddWithValue("$pid", partId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertStepsAsync(
        SqliteConnection conn, System.Data.Common.DbTransaction tx,
        long partId, List<ProcessStep> steps)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = (SqliteTransaction)tx;
        cmd.CommandText = """
            INSERT INTO process_template (part_id, row_number, shop_code, description)
            VALUES ($pid, $row, $shop, $desc)
            """;

        var pPid  = cmd.Parameters.Add("$pid",  SqliteType.Integer);
        var pRow  = cmd.Parameters.Add("$row",  SqliteType.Integer);
        var pShop = cmd.Parameters.Add("$shop", SqliteType.Text);
        var pDesc = cmd.Parameters.Add("$desc", SqliteType.Text);

        foreach (var step in steps)
        {
            pPid.Value  = partId;
            pRow.Value  = step.RowNumber;
            pShop.Value = step.ShopCode;
            pDesc.Value = step.ProcessDescription;
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
