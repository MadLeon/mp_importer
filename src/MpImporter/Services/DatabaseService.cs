using Microsoft.Data.Sqlite;
using MpImporter.Models;
using Serilog;

namespace MpImporter.Services;

public class DatabaseService
{
    private record PartRecord(long Id, string Revision, string? Description);

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

        var part = await FindPartAsync(conn, result.DrawingNumber, result.Revision);

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            long partId;
            if (part == null)
            {
                partId = await InsertPartAsync(conn, tx, result);
                Log.Information("Inserted new part: {Drawing} rev {Rev}, part_id={PartId}",
                    result.DrawingNumber, result.Revision, partId);
                await InsertStepsAsync(conn, tx, partId, result.ProcessSteps);
                Log.Information("Inserted {Count} process step(s) for part_id={PartId}",
                    result.ProcessSteps.Count, partId);
            }
            else
            {
                partId = part.Id;

                // Handle revision
                var dbRev = part.Revision;
                var extRev = result.Revision;
                if ((string.IsNullOrWhiteSpace(dbRev) || dbRev == "-") && !string.IsNullOrWhiteSpace(extRev))
                {
                    await UpdatePartFieldAsync(conn, tx, partId, "revision", extRev);
                    Log.Information("Updated revision for part_id={PartId} ({Drawing}): '{Old}' → '{New}'",
                        partId, result.DrawingNumber, dbRev, extRev);
                }
                else if (dbRev != extRev)
                {
                    Log.Warning("Revision mismatch for {Drawing}: DB='{DbRev}' extracted='{ExtRev}' — not modified",
                        result.DrawingNumber, dbRev, extRev);
                }

                // Handle description
                var dbDesc = part.Description;
                var extDesc = result.Description;
                if (string.IsNullOrWhiteSpace(dbDesc) && !string.IsNullOrWhiteSpace(extDesc))
                {
                    await UpdatePartFieldAsync(conn, tx, partId, "description", extDesc);
                    Log.Information("Updated description for part_id={PartId} ({Drawing}): → '{New}'",
                        partId, result.DrawingNumber, extDesc);
                }
                else if (!string.IsNullOrWhiteSpace(dbDesc) && dbDesc != extDesc)
                {
                    Log.Warning("Description mismatch for {Drawing}: DB='{DbDesc}' extracted='{ExtDesc}' — not modified",
                        result.DrawingNumber, dbDesc, extDesc);
                }

                // Handle process_template
                if (await HasProcessTemplatesAsync(conn, partId))
                {
                    Log.Warning("Process templates already exist for part_id={PartId} ({Drawing}) — skipping insertion",
                        partId, result.DrawingNumber);
                }
                else
                {
                    await InsertStepsAsync(conn, tx, partId, result.ProcessSteps);
                    Log.Information("Inserted {Count} process step(s) for part_id={PartId}",
                        result.ProcessSteps.Count, partId);
                }
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Searches by drawing_number only. Returns null if no matching part exists.
    /// </summary>
    private static async Task<PartRecord?> FindPartAsync(
        SqliteConnection conn, string drawingNumber, string revision)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, revision, description
            FROM part
            WHERE drawing_number = $dn
            ORDER BY id
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$dn", drawingNumber);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new PartRecord(
            Id:          reader.GetInt64(0),
            Revision:    reader.GetString(1),
            Description: reader.IsDBNull(2) ? null : reader.GetString(2));
    }

    private static async Task<long> InsertPartAsync(
        SqliteConnection conn, System.Data.Common.DbTransaction tx, ExtractionResult result)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = (SqliteTransaction)tx;
        cmd.CommandText = """
            INSERT INTO part (drawing_number, revision, description)
            VALUES ($dn, $rev, $desc)
            RETURNING id
            """;
        cmd.Parameters.AddWithValue("$dn",   result.DrawingNumber);
        cmd.Parameters.AddWithValue("$rev",  string.IsNullOrWhiteSpace(result.Revision) ? "-" : result.Revision);
        cmd.Parameters.AddWithValue("$desc", string.IsNullOrWhiteSpace(result.Description) ? (object)DBNull.Value : result.Description);

        var id = await cmd.ExecuteScalarAsync();
        return (long)id!;
    }

    private static async Task UpdatePartFieldAsync(
        SqliteConnection conn, System.Data.Common.DbTransaction tx,
        long partId, string column, string value)
    {
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = (SqliteTransaction)tx;
        cmd.CommandText = $"""
            UPDATE part SET {column} = $val, updated_at = datetime('now', 'localtime')
            WHERE id = $id
            """;
        cmd.Parameters.AddWithValue("$val", value);
        cmd.Parameters.AddWithValue("$id",  partId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<bool> HasProcessTemplatesAsync(
        SqliteConnection conn, long partId)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM process_template WHERE part_id = $pid";
        cmd.Parameters.AddWithValue("$pid", partId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        return count > 0;
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
