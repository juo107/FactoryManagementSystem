using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Materials;

namespace FactoryManagementSystem.Services
{
    public class MaterialsService : IMaterialsService
    {
        private readonly IConfiguration _config;
        private readonly IRedisCacheService _cache;

        public MaterialsService(IConfiguration config, IRedisCacheService cache)
        {
            _config = config;
            _cache = cache;
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

        public async Task<ApiResponse<IEnumerable<MaterialProductionOrderDto>>> GetProductionOrdersAsync(IQueryCollection query)
        {
            string queryStr = string.Join("_", query.Select(kv => kv.Key + "=" + kv.Value));
            string cacheKey = $"materials:orders:{queryStr}";
            var cached = await _cache.GetAsync<ApiResponse<IEnumerable<MaterialProductionOrderDto>>>(cacheKey);
            if (cached != null) return cached;

            var p = new DynamicParameters();
            var where = new List<string>();
            ApplyDateFilter(query, p, where, "");
            var whereClause = where.Any() ? "WHERE " + string.Join(" AND ", where) : "";

            var sql = $@"
                SELECT DISTINCT productionOrderNumber AS ProductionOrderNumber
                FROM MESMaterialConsumption WITH (NOLOCK)
                {whereClause}
                ORDER BY ProductionOrderNumber ASC
            ";

            using var conn = Connection;
            var rows = await conn.QueryAsync<MaterialProductionOrderDto>(sql, p);
            var result = ApiResponse<IEnumerable<MaterialProductionOrderDto>>.Success(rows);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<IEnumerable<MaterialBatchDto>>> GetBatchCodesAsync(string? productionOrderNumber, IQueryCollection query)
        {
            string queryStr = string.Join("_", query.Select(kv => kv.Key + "=" + kv.Value));
            string cacheKey = $"materials:batches:{productionOrderNumber}:{queryStr}";
            var cached = await _cache.GetAsync<ApiResponse<IEnumerable<MaterialBatchDto>>>(cacheKey);
            if (cached != null) return cached;

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
                SELECT batchCode AS BatchCode FROM (
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
            var rows = await conn.QueryAsync<MaterialBatchDto>(sql, p);
            var result = ApiResponse<IEnumerable<MaterialBatchDto>>.Success(rows);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<IEnumerable<MaterialIngredientDto>>> GetIngredientsAsync(string? productionOrderNumber, string? batchCode, IQueryCollection query)
        {
            string queryStr = string.Join("_", query.Select(kv => kv.Key + "=" + kv.Value));
            string cacheKey = $"materials:ingredients:{productionOrderNumber}:{batchCode}:{queryStr}";
            var cached = await _cache.GetAsync<ApiResponse<IEnumerable<MaterialIngredientDto>>>(cacheKey);
            if (cached != null) return cached;

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
                SELECT DISTINCT ingredientCode AS IngredientCode
                FROM MESMaterialConsumption WITH (NOLOCK)
                WHERE {string.Join(" AND ", where)}
                ORDER BY IngredientCode ASC
            ";

            using var conn = Connection;
            var rows = await conn.QueryAsync<MaterialIngredientDto>(sql, p);
            var result = ApiResponse<IEnumerable<MaterialIngredientDto>>.Success(rows);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<IEnumerable<MaterialSimpleDto>>> GetShiftsAsync(IQueryCollection query)
        {
            string queryStr = string.Join("_", query.Select(kv => kv.Key + "=" + kv.Value));
            string cacheKey = $"materials:shifts:{queryStr}";
            var cached = await _cache.GetAsync<ApiResponse<IEnumerable<MaterialSimpleDto>>>(cacheKey);
            if (cached != null) return cached;

            var p = new DynamicParameters();
            var dateWhere = new List<string>();
            ApplyDateFilter(query, p, dateWhere, "mmc");

            string sql;
            if (dateWhere.Any())
            {
                sql = $@"
                    SELECT DISTINCT po.Shift AS Value
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
                    SELECT DISTINCT Shift AS Value
                    FROM ProductionOrders WITH (NOLOCK)
                    WHERE Shift IS NOT NULL AND LTRIM(RTRIM(Shift)) <> ''
                    ORDER BY Shift ASC
                ";
            }

            using var conn = Connection;
            var rows = await conn.QueryAsync<MaterialSimpleDto>(sql, p);
            var result = ApiResponse<IEnumerable<MaterialSimpleDto>>.Success(rows);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<PagedResponse<MaterialConsumptionDto>>> SearchAsync(IQueryCollection query, int page, int pageSize)
        {
            var pageNum = Math.Max(page, 1);
            var pageLimit = Math.Max(pageSize, 1);
            var offset = (pageNum - 1) * pageLimit;

            string queryStr = string.Join("_", query.Select(kv => kv.Key + "=" + kv.Value));
            string cacheKey = $"materials:search:{queryStr}:{page}:{pageSize}";
            var cached = await _cache.GetAsync<ApiResponse<PagedResponse<MaterialConsumptionDto>>>(cacheKey);
            if (cached != null) return cached;

            var p = new DynamicParameters();
            var whereClause = BuildSearchWhere(query, p);

            var sql = $@"
                SELECT 
                    mmc.id AS Id, mmc.productionOrderNumber AS ProductionOrderNumber, mmc.batchCode AS BatchCode,
                    CAST(CAST(mmc.quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity, mmc.ingredientCode AS IngredientCode, pm.ItemName AS IngredientName,
                    mmc.lot AS Lot, mmc.unitOfMeasurement AS UnitOfMeasurement, mmc.datetime AS Datetime,
                    mmc.operator_ID AS Operator_ID, mmc.supplyMachine AS SupplyMachine,
                    mmc.respone AS Respone, mmc.status AS Status, mmc.status1 AS Status1,
                    mmc.request AS Request, mmc.count AS Count, mmc.timestamp AS Timestamp,
                    po.Shift AS Shift, po.ProductionLine AS ProductionLine
                FROM MESMaterialConsumption mmc WITH (NOLOCK)
                LEFT JOIN ProductionOrders po WITH (NOLOCK)
                  ON mmc.productionOrderNumber = po.ProductionOrderNumber
                LEFT JOIN ProductMasters pm WITH (NOLOCK)
                  ON pm.ItemCode = mmc.ingredientCode
                {whereClause}
                ORDER BY mmc.datetime DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
            ";

            p.Add("offset", offset);
            p.Add("limit", pageLimit);

            using var conn = Connection;
            var dtos = (await conn.QueryAsync<MaterialConsumptionDto>(sql, p)).ToList();
            
            var result = ApiResponse<PagedResponse<MaterialConsumptionDto>>.Success(
                new PagedResponse<MaterialConsumptionDto>(dtos, dtos.Count, pageNum, pageLimit)
            );

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
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
                if (ingValues.Contains("NULL")) conditions.Add("mmc.ingredientCode IS NULL");
                var real = ingValues.Where(v => v != "NULL").ToList();
                for (int i = 0; i < real.Count; i++)
                {
                    p.Add($"ing{i}", $"%{real[i]}%");
                    conditions.Add("mmc.ingredientCode LIKE @ing" + i);
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

        public async Task<ApiResponse<object>> GetStatsSearchAsync(IQueryCollection query)
        {
            string queryStr = string.Join("_", query.Select(kv => kv.Key + "=" + kv.Value));
            string cacheKey = $"materials:stats:{queryStr}";
            var cached = await _cache.GetAsync<ApiResponse<object>>(cacheKey);
            if (cached != null) return cached;

            var p = new DynamicParameters();
            var whereClause = BuildSearchWhere(query, p);

            var sql = $@"
                SELECT 
                    COUNT(*) AS Total,
                    SUM(CASE WHEN mmc.respone = 'Success' THEN 1 ELSE 0 END) AS SuccessCount,
                    SUM(CASE WHEN mmc.respone <> 'Success' OR mmc.respone IS NULL THEN 1 ELSE 0 END) AS FailedCount
                FROM MESMaterialConsumption mmc WITH (NOLOCK)
                LEFT JOIN ProductionOrders po WITH (NOLOCK)
                  ON mmc.productionOrderNumber = po.ProductionOrderNumber
                {whereClause}
            ";

            using var conn = Connection;
            var stats = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, p);
            var result = ApiResponse<object>.Success(new 
            { 
                Total = stats?.Total ?? 0,
                Success = stats?.SuccessCount ?? 0,
                Failed = stats?.FailedCount ?? 0
            });
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }
    }
}
