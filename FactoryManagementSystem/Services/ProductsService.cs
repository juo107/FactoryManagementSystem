using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace FactoryManagementSystem.Services
{
    public class ProductsService : IProductsService
    {
        private readonly IConfiguration _config;

        public ProductsService(IConfiguration config)
        {
            _config = config;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<object> GetTypesAsync()
        {
            var sql = @"SELECT DISTINCT Item_Type FROM ProductMasters";
            using var conn = Connection;
            var rows = await conn.QueryAsync<string>(sql);
            return rows.ToList();
        }

        public async Task<object> SearchAsync(string? q, string? status, string? statuses, string? type, string? types, int page, int pageSize)
        {
            var pageInt = Math.Max(page, 1);
            var pageSizeInt = Math.Min(Math.Max(pageSize, 1), 100);
            var offset = (pageInt - 1) * pageSizeInt;

            var whereClauses = new List<string>();
            var p = new DynamicParameters();

            var statusUpper = (status ?? "").ToUpper();
            var statusesList = (statuses ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToList();
            var typesList = (types ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                whereClauses.Add("(p.ItemCode LIKE @q OR p.ItemName LIKE @q OR p.[Group] LIKE @q)");
                p.Add("q", $"%{q}%");
            }

            if (statusesList.Any())
            {
                var parts = new List<string>();
                for (int i = 0; i < statusesList.Count; i++)
                {
                    p.Add($"status{i}", statusesList[i]);
                    if (statusesList[i] == "ACTIVE") parts.Add($"p.Item_Status = @status{i}");
                    else parts.Add($"(p.Item_Status = @status{i} OR p.Item_Status IS NULL)");
                }
                whereClauses.Add($"({string.Join(" OR ", parts)})");
            }
            else if (statusUpper == "ACTIVE")
            {
                whereClauses.Add("p.Item_Status = @status");
                p.Add("status", "ACTIVE");
            }
            else if (statusUpper == "INACTIVE")
            {
                whereClauses.Add("(p.Item_Status = @inactiveStatus OR p.Item_Status IS NULL)");
                p.Add("inactiveStatus", "INACTIVE");
            }

            if (typesList.Any())
            {
                var placeholders = new List<string>();
                for (int i = 0; i < typesList.Count; i++)
                {
                    var key = $"type{i}";
                    placeholders.Add("@" + key);
                    p.Add(key, typesList[i]);
                }
                whereClauses.Add($"p.Item_Type IN ({string.Join(", ", placeholders)})");
            }
            else if (!string.IsNullOrWhiteSpace(type))
            {
                whereClauses.Add("p.Item_Type = @type");
                p.Add("type", type);
            }

            var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
            using var conn = Connection;

            var total = await conn.ExecuteScalarAsync<int>($@"SELECT COUNT(*) FROM ProductMasters p {whereSql}", p);

            var dataSql = $@"
                SELECT 
                    p.ProductMasterId, p.ItemCode, p.ItemName, p.Item_Type, p.[Group], p.Category, p.Brand, 
                    p.BaseUnit, p.InventoryUnit, p.Item_Status, p.[timestamp],
                    JSON_QUERY((
                        SELECT m.MHUTypeId, m.FromUnit, m.ToUnit, m.Conversion 
                        FROM MHUTypes m WHERE m.ProductMasterId = p.ProductMasterId 
                        FOR JSON PATH
                    )) AS MhuTypes 
                FROM ProductMasters p 
                {whereSql} 
                ORDER BY p.[timestamp] DESC 
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            p.Add("offset", offset);
            p.Add("pageSize", pageSizeInt);

            var rows = await conn.QueryAsync(dataSql, p);
            var items = rows.Select(r =>
            {
                var rowDict = (IDictionary<string, object>)r;
                string json = rowDict["MhuTypes"]?.ToString() ?? "[]";
                rowDict["MhuTypes"] = JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();
                return rowDict;
            });

            return new { items, total, page = pageInt, pageSize = pageSizeInt };
        }

        public async Task<object> GetStatsSearchAsync(string? q, string? status, string? statuses, string? type, string? types)
        {
            var where = new List<string>();
            var p = new DynamicParameters();
            var statusesList = (statuses ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToList();
            var typesList = (types ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            if (!string.IsNullOrWhiteSpace(q)) { where.Add("(p.ItemCode LIKE @q OR p.ItemName LIKE @q OR p.[Group] LIKE @q)"); p.Add("q", $"%{q}%"); }
            if (statusesList.Any())
            {
                var parts = new List<string>();
                for (int i = 0; i < statusesList.Count; i++) { p.Add($"status{i}", statusesList[i]); if (statusesList[i] == "ACTIVE") parts.Add($"p.Item_Status = @status{i}"); else parts.Add($"(p.Item_Status = @status{i} OR p.Item_Status IS NULL)"); }
                where.Add($"({string.Join(" OR ", parts)})");
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToUpper() == "ACTIVE") { where.Add("p.Item_Status = @status"); p.Add("status", "ACTIVE"); }
                else if (status.ToUpper() == "INACTIVE") { where.Add("(p.Item_Status = @inactive OR p.Item_Status IS NULL)"); p.Add("inactive", "INACTIVE"); }
            }

            if (typesList.Any()) { where.Add("p.Item_Type IN @typesList"); p.Add("typesList", typesList); }
            else if (!string.IsNullOrWhiteSpace(type)) { where.Add("p.Item_Type = @type"); p.Add("type", type); }

            var whereSql = where.Any() ? $"WHERE {string.Join(" AND ", where)}" : "";
            var sql = $@"SELECT COUNT(*) AS totalProducts, SUM(CASE WHEN p.Item_Status = 'ACTIVE' THEN 1 ELSE 0 END) AS activeProducts, COUNT(DISTINCT p.Item_Type) AS totalTypes, COUNT(DISTINCT p.Category) AS totalCategories, COUNT(DISTINCT p.[Group]) AS totalGroups FROM ProductMasters p {whereSql}";
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync(sql, p);
        }

        public async Task<object> GetByIdAsync(string id)
        {
            var sql = @"
                SELECT 
                    p.ProductMasterId, p.ItemCode, p.ItemName, p.Item_Type, p.[Group], p.Category, p.Brand, 
                    p.BaseUnit, p.InventoryUnit, p.Item_Status, p.timestamp,
                    JSON_QUERY((
                        SELECT m.MHUTypeId, m.FromUnit, m.ToUnit, m.Conversion 
                        FROM MHUTypes m WHERE m.ProductMasterId = p.ProductMasterId 
                        FOR JSON PATH
                    )) AS MhuTypes 
                FROM ProductMasters p 
                WHERE p.ItemCode = @id";

            using var conn = Connection;
            var row = await conn.QueryFirstOrDefaultAsync(sql, new { id });
            if (row == null) return null;

            var rowDict = (IDictionary<string, object>)row;
            string json = rowDict["MhuTypes"]?.ToString() ?? "[]";
            var mhuTypes = JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();

            rowDict.Remove("MhuTypes");
            rowDict.Add("MhuTypes", mhuTypes);

            return rowDict;
        }
    }
}
