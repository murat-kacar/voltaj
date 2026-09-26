using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Api.Endpoints;

public static class TestDataEndpoints
{
    private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppRoles", "AppUserRoles", "AppUsers", "AuditEvents", "AuditLogs",
        "BillingEntries", "CashShifts", "CommandRecords", "CustomerAssets",
        "CustomerLedgerEntries", "CustomerPayments", "CustomerSites", "Customers",
        "DocumentCounters", "ExecutionGuards", "MaintenanceContracts", "MaterialStocks",
        "OperationTraces", "OutboxMessages", "PasswordResetTokens",
        "PaymentInvoiceAllocations", "Products", "ProgressBillings", "ProjectPhase",
        "Projects", "QuickSaleLines", "QuickSalePayments", "QuickSaleReturnLines",
        "QuickSaleReturns", "QuickSales", "QuoteItem", "Quotes", "ReferenceValues",
        "ReminderRecords", "SalesInvoices", "StockMovements", "UserSessions",
        "Warehouses", "WorkOrderItem", "WorkOrderTimeEntry", "WorkOrders"
    };

    private static readonly Regex SafeColumnRegex = new("^[a-zA-Z0-9_]+$", RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapTestDataEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/test-data").WithTags("99-TestData").AllowAnonymous();

        group.MapGet("/tables", async (VoltflowDbContext db, CancellationToken ct) =>
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT relname as ""TableName"", n_live_tup as ""RowCount""
                FROM pg_stat_user_tables
                WHERE relname != '__EFMigrationsHistory';";

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var table = reader.GetString(0);
                var count = reader.GetInt64(1);
                result[table] = count;
            }

            // Ensure all 41 tables are present in the dictionary even if not yet analyzed by stats
            foreach (var t in AllowedTables)
            {
                if (!result.ContainsKey(t))
                    result[t] = 0;
            }

            return Results.Ok(result);
        })
        .WithName("VF-99001_GetTableCounts");

        group.MapPost("/insert", async (InsertTestDataRequest request, VoltflowDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TableName) || !AllowedTables.TryGetValue(request.TableName, out var tableName))
            {
                return Results.BadRequest(new { Error = $"Geçersiz tablo adı: {request.TableName}" });
            }

            if (request.Data == null || request.Data.Count == 0)
            {
                return Results.BadRequest(new { Error = "Kayıt verisi boş olamaz." });
            }

            var conn = db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            // Prepare record data
            var data = new Dictionary<string, object?>(request.Data, StringComparer.OrdinalIgnoreCase);

            // Auto-populate primary key if omitted
            if (!data.ContainsKey("Id"))
            {
                data["Id"] = Guid.NewGuid();
            }

            // Auto-populate CreatedAt if table usually has it
            if (!data.ContainsKey("CreatedAt") && tableName != "ProjectPhase" && tableName != "QuoteItem" && tableName != "WorkOrderItem")
            {
                data["CreatedAt"] = DateTime.UtcNow;
            }

            // Auto-populate Version if omitted
            if (!data.ContainsKey("Version") && tableName != "ProjectPhase" && tableName != "QuoteItem" && tableName != "WorkOrderItem" && tableName != "WorkOrderTimeEntry")
            {
                data["Version"] = 1L;
            }

            // If AppUsers and password provided in plain text, provide a test hash
            if (string.Equals(tableName, "AppUsers", StringComparison.OrdinalIgnoreCase))
            {
                if (!data.ContainsKey("PasswordHash") || string.IsNullOrWhiteSpace(data["PasswordHash"]?.ToString()))
                {
                    data["PasswordHash"] = "AQAAAAIAAYagAAAAENoneHashForTesting123!";
                }
            }

            // Build parameterized SQL
            var columns = new List<string>();
            var paramNames = new List<string>();
            var paramList = new List<DbParameter>();
            var pIndex = 0;

            foreach (var (col, val) in data)
            {
                if (!SafeColumnRegex.IsMatch(col))
                {
                    return Results.BadRequest(new { Error = $"Güvensiz kolon adı: {col}" });
                }

                var paramName = $"@p{pIndex++}";
                columns.Add($"\"{col}\"");
                paramNames.Add(paramName);

                var p = conn.CreateCommand().CreateParameter();
                p.ParameterName = paramName;

                // Handle JSON, numbers, guids and dates cleanly
                if (val is null)
                {
                    p.Value = DBNull.Value;
                }
                else if (val is System.Text.Json.JsonElement elem)
                {
                    p.Value = elem.ValueKind switch
                    {
                        System.Text.Json.JsonValueKind.String when Guid.TryParse(elem.GetString(), out var g) => g,
                        System.Text.Json.JsonValueKind.String when DateTime.TryParse(elem.GetString(), out var dt) => dt,
                        System.Text.Json.JsonValueKind.String => elem.GetString(),
                        System.Text.Json.JsonValueKind.Number when elem.TryGetInt64(out var i) => i,
                        System.Text.Json.JsonValueKind.Number when elem.TryGetDecimal(out var d) => d,
                        System.Text.Json.JsonValueKind.True => true,
                        System.Text.Json.JsonValueKind.False => false,
                        System.Text.Json.JsonValueKind.Null => DBNull.Value,
                        _ => elem.GetRawText()
                    };
                }
                else
                {
                    p.Value = val;
                }

                paramList.Add(p);
            }

            var sql = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)});";

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                foreach (var p in paramList)
                {
                    cmd.Parameters.Add(p);
                }

                await cmd.ExecuteNonQueryAsync(ct);

                var recordId = data.TryGetValue("Id", out var idVal) ? idVal?.ToString() : "OK";
                return Results.Ok(new
                {
                    Success = true,
                    Table = tableName,
                    Id = recordId,
                    Message = $"{tableName} tablosuna kayıt başarıyla eklendi."
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    Success = false,
                    Error = ex.Message,
                    Table = tableName
                });
            }
        })
        .WithName("VF-99002_InsertTestData");

        return routes;
    }
}

public sealed record InsertTestDataRequest(string TableName, Dictionary<string, object?> Data);
