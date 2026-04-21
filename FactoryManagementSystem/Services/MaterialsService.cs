using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FactoryManagementSystem.Services
{
    public class MaterialsService : IMaterialsService
    {
        private readonly IConfiguration _config;

        public MaterialsService(IConfiguration config)
        {
            _config = config;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        private void ApplyDateFilter(IQueryCollection query, DynamicParameters p, List<string> where, string alias = "mmc")
        {
            string col = string.IsNullOrWhiteSpace(alias) ? "datetime" : $"{alias}.datetime";

            DateTime? ParseDate(string input, bool endOfDay = false)
            {
                if (string.IsNullOrWhiteSpace(input)) return null;
                if (DateTime.TryParse(input, out var dt))
                {
                    if (input.Length == 10) // yyyy-MM-dd
                    {
                        return endOfDay ? dt.Date.AddDays(1).AddMilliseconds(-1) : dt.Date;
                    }
                    return dt;
                }
                return null;
            }

            var fromStr = query["fromDate"].ToString();
            var toStr = query["toDate"].ToString();
            var from = ParseDate(fromStr);
            var to = ParseDate(toStr, true);

            if (from.HasValue && to.HasValue) { p.Add("fromDate", from); p.Add("toDate", to); where.Add($"{col} BETWEEN @fromDate AND @toDate"); }
            else if (from.HasValue) { p.Add("fromDate", from); where.Add($"{col} >= @fromDate"); }
            else if (to.HasValue) { p.Add("toDate", to); where.Add($"{col} <= @toDate"); }
        }

        private List<string> NormalizeQuery(object input)
        {
            if (input?.ToString() is not string rawInput) return new();
            return rawInput.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        public async Task<object> GetProductionOrdersAsync(IQueryCollection query)
        {
            var p = new DynamicParameters();
            var where = new List<string>();
            ApplyDateFilter(query, p, where, "");
            var whereClause = where.Any() ? "WHERE " + string.Join(" AND ", where) : "";

            var sql = $@"
                SELECT DISTINCT productionOrderNumber
                FROM MESMaterialConsumption WITH (NOLOCK)
                {whereClause}
                ORDER BY productionOrderNumber ASC
            ";

            using var conn = Connection;
            var rows = await conn.QueryAsync<string>(sql, p);
            return new {
                success = true,
                message = "Success",
                data = rows.Select(r => new { productionOrderNumber = r })
            };
        }

        public async Task<object> GetBatchCodesAsync(string? productionOrderNumber, IQueryCollection query)
        {
            var p = new DynamicParameters();
            var extraWhere = new List<string>();

            if (!string.IsNullOrWhiteSpace(productionOrderNumber))
            {
                p.Add("po", $"%{productionOrderNumber.Trim()}%");
                extraWhere.Add("productionOrderNumber LIKE @po");
            }

            ApplyDateFilter(query, p, extraWhere, "");
            var extraStr = extraWhere.Any() ? "AND " + string.Join(" AND ", extraWhere) : "";

            var sql = $@"
                SELECT batchCode FROM (
                    SELECT DISTINCT
                        CASE WHEN batchCode IS NULL OR LTRIM(RTRIM(batchCode)) = '' THEN NULL ELSE batchCode END AS batchCode,
                        CASE WHEN batchCode IS NULL OR LTRIM(RTRIM(batchCode)) = '' THEN 0 ELSE 1 END AS sort_grp
                    FROM MESMaterialConsumption WITH (NOLOCK)
                    WHERE 1=1
                    {extraStr}
                ) combined
                ORDER BY sort_grp ASC, TRY_CAST(batchCode AS INT) ASC;
            ";

            using var conn = Connection;
            var rows = await conn.QueryAsync<string>(sql, p);
            return new {
                success = true,
                message = "Success",
                data = rows.Select(r => new { batchCode = r })
            };
        }

        public async Task<object> GetIngredientsAsync(string? productionOrderNumber, string? batchCode, IQueryCollection query)
        {
            var p = new DynamicParameters();
            var where = new List<string> { "ingredientCode IS NOT NULL", "LTRIM(RTRIM(ingredientCode)) <> ''" };

            if (!string.IsNullOrWhiteSpace(productionOrderNumber))
            {
                p.Add("po", $"%{productionOrderNumber.Trim()}%");
                where.Add("productionOrderNumber LIKE @po");
            }

            if (!string.IsNullOrWhiteSpace(batchCode))
            {
                p.Add("bc", $"%{batchCode.Trim()}%");
                where.Add("batchCode LIKE @bc");
            }

            ApplyDateFilter(query, p, where, "");

            var sql = $@"
                SELECT DISTINCT ingredientCode
                FROM MESMaterialConsumption WITH (NOLOCK)
                WHERE {string.Join(" AND ", where)}
                ORDER BY ingredientCode ASC
            ";

            using var conn = Connection;
            var rows = await conn.QueryAsync<string>(sql, p);
            return new {
                success = true,
                message = "Success",
                data = rows.Select(r => new { ingredientCode = r })
            };
        }

        public async Task<object> GetShiftsAsync(IQueryCollection query)
        {
            var p = new DynamicParameters();
            var dateWhere = new List<string>();
            ApplyDateFilter(query, p, dateWhere, "mmc");

            string sql;
            if (dateWhere.Any())
            {
                sql = $@"
                    SELECT DISTINCT po.Shift
                    FROM ProductionOrders po WITH (NOLOCK)
                    INNER JOIN MESMaterialConsumption mmc WITH (NOLOCK)
                      ON po.ProductionOrderNumber = mmc.productionOrderNumber
                    WHERE po.Shift IS NOT NULL AND LTRIM(RTRIM(po.Shift)) <> ''
                      AND {string.Join(" AND ", dateWhere)}
                    ORDER BY po.Shift ASC
                ";
            }
            else
            {
                sql = @"
                    SELECT DISTINCT Shift
                    FROM ProductionOrders WITH (NOLOCK)
                    WHERE Shift IS NOT NULL AND LTRIM(RTRIM(Shift)) <> ''
                    ORDER BY Shift ASC
                ";
            }

            using var conn = Connection;
            var rows = await conn.QueryAsync<string>(sql, p);
            return new {
                success = true,
                message = "Success",
                data = rows.Select(r => new { shift = r })
            };
        }

        public async Task<object> SearchAsync(IQueryCollection query, int page, int pageSize)
        {
            var pageNum = Math.Max(page, 1);
            var pageLimit = Math.Max(pageSize, 1);
            var offset = (pageNum - 1) * pageLimit;

            var p = new DynamicParameters();
            var whereClause = BuildSearchWhere(query, p);

            var sql = $@"
                SELECT mmc.*, po.Shift AS shift, po.ProductionLine AS productionLine
                FROM MESMaterialConsumption mmc WITH (NOLOCK)
                LEFT JOIN ProductionOrders po WITH (NOLOCK)
                  ON mmc.productionOrderNumber = po.ProductionOrderNumber
                {whereClause}
                ORDER BY mmc.datetime DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
            ";

            p.Add("offset", offset);
            p.Add("limit", pageLimit);

            using var conn = Connection;
            var rows = await conn.QueryAsync(sql, p);
            return new {
                success = true,
                message = "Success",
                data = rows
            };
        }

        private string BuildSearchWhere(IQueryCollection query, DynamicParameters p)
        {
            var where = new List<string>();

            // Production Orders
            var poValues = NormalizeQuery(query["productionOrderNumber"]);
            if (poValues.Any())
            {
                var conditions = new List<string>();
                if (poValues.Contains("NULL")) conditions.Add("mmc.productionOrderNumber = ''");
                var real = poValues.Where(v => v != "NULL").ToList();
                for (int i = 0; i < real.Count; i++)
                {
                    p.Add($"po{i}", $"%{real[i]}%");
                    conditions.Add($"mmc.productionOrderNumber LIKE @po{i}");
                }
                if (conditions.Any()) where.Add($"({string.Join(" OR ", conditions)})");
            }

            // Batch Codes
            var bcValues = NormalizeQuery(query["batchCode"]);
            if (bcValues.Any())
            {
                var conditions = new List<string>();
                if (bcValues.Contains("NULL")) conditions.Add("(mmc.batchCode IS NULL OR LTRIM(RTRIM(mmc.batchCode)) = '')");
                var real = bcValues.Where(v => v != "NULL").ToList();
                for (int i = 0; i < real.Count; i++)
                {
                    p.Add($"bc{i}", real[i]);
                    conditions.Add($"mmc.batchCode = @bc{i}");
                }
                if (conditions.Any()) where.Add($"({string.Join(" OR ", conditions)})");
            }

            // Ingredient Codes
            var ingValues = NormalizeQuery(query["ingredientCode"]);
            if (ingValues.Any())
            {
                var conditions = new List<string>();
                if (ingValues.Contains("NULL")) conditions.Add("ingredientCode IS NULL");
                var real = ingValues.Where(v => v != "NULL").ToList();
                for (int i = 0; i < real.Count; i++)
                {
                    p.Add($"ing{i}", $"%{real[i]}%");
                    conditions.Add($"ingredientCode LIKE @ing{i}");
                }
                if (conditions.Any()) where.Add($"({string.Join(" OR ", conditions)})");
            }

            // Response/Status
            var respValues = NormalizeQuery(query["respone"]);
            if (respValues.Contains("Success") && !respValues.Contains("Failed"))
            {
                where.Add("mmc.respone = 'Success'");
            }
            else if (!respValues.Contains("Success") && respValues.Contains("Failed"))
            {
                where.Add("(mmc.respone <> 'Success' OR mmc.respone IS NULL)");
            }

            // Shift
            var shiftValues = NormalizeQuery(query["shift"]);
            if (shiftValues.Any())
            {
                var placeholders = new List<string>();
                for (int i = 0; i < shiftValues.Count; i++)
                {
                    var key = $"shift{i}";
                    placeholders.Add("@" + key);
                    p.Add(key, shiftValues[i]);
                }
                where.Add($"po.Shift IN ({string.Join(",", placeholders)})");
            }

            ApplyDateFilter(query, p, where);

            return where.Any() ? "WHERE " + string.Join(" AND ", where) : "";
        }

        public async Task<object> GetStatsSearchAsync(IQueryCollection query)
        {
            var p = new DynamicParameters();
            var whereClause = BuildSearchWhere(query, p);

            var sql = $@"
                SELECT COUNT(*) AS total
                FROM MESMaterialConsumption mmc WITH (NOLOCK)
                LEFT JOIN ProductionOrders po WITH (NOLOCK)
                  ON mmc.productionOrderNumber = po.ProductionOrderNumber
                {whereClause}
            ";

            using var conn = Connection;
            var total = await conn.ExecuteScalarAsync<int>(sql, p);
            return new {
                success = true,
                message = "Success",
                data = new { total }
            };
        }
    }
}
