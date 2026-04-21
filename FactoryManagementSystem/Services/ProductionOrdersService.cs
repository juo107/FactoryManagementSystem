using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Generic;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.ProductionOrders;

namespace FactoryManagementSystem.Services
{
    public class ProductionOrdersService : IProductionOrdersService
    {
        private readonly IConfiguration _config;
        private readonly IRedisCacheService _cache;

        public ProductionOrdersService(IConfiguration config, IRedisCacheService cache)
        {
            _config = config;
            _cache = cache;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<ApiResponse<OrderFiltersDto>> GetFiltersAsync(string dateFrom, string dateTo)
        {
            string cacheKey = $"production_orders:filters:{dateFrom}:{dateTo}";
            var cachedData = await _cache.GetAsync<ApiResponse<OrderFiltersDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            using var conn = Connection;
            var where = new List<string>();
            var p = new DynamicParameters();

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime df))
            {
                where.Add("PlannedStart >= @dateFrom");
                p.Add("dateFrom", df.Date);
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime dt))
            {
                where.Add("PlannedStart < DATEADD(day, 1, @dateTo)");
                p.Add("dateTo", dt.Date);
            }

            string whereClause = where.Count > 0 ? "AND " + string.Join(" AND ", where) : "";

            var sql = $@"
                SELECT DISTINCT ProcessArea FROM ProductionOrders 
                WHERE ProcessArea IS NOT NULL AND LTRIM(RTRIM(ProcessArea)) <> '' {whereClause};

                SELECT DISTINCT Shift FROM ProductionOrders 
                WHERE Shift IS NOT NULL AND LTRIM(RTRIM(Shift)) <> '' {whereClause};
            ";

            using var multi = await conn.QueryMultipleAsync(sql, p);
            var result = ApiResponse<OrderFiltersDto>.Success(new OrderFiltersDto
            {
                ProcessAreas = await multi.ReadAsync<string>(),
                Shifts = await multi.ReadAsync<string>()
            });

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<OrderFiltersDto>> GetFiltersV2Async(string dateFrom, string dateTo)
        {
            string cacheKey = $"production_orders:filters_v2:{dateFrom}:{dateTo}";
            var cachedData = await _cache.GetAsync<ApiResponse<OrderFiltersDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            using var conn = Connection;
            var where = new List<string>();
            var p = new DynamicParameters();

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime df))
            {
                where.Add("PlannedStart >= @dateFrom");
                p.Add("dateFrom", df.Date);
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime dt))
            {
                where.Add("PlannedStart < DATEADD(day, 1, @dateTo)");
                p.Add("dateTo", dt.Date);
            }

            string whereClause = where.Count > 0 ? " AND " + string.Join(" AND ", where) : "";

            var sql = $@"
                SELECT DISTINCT ProcessArea FROM ProductionOrders 
                WHERE ProcessArea IS NOT NULL AND LTRIM(RTRIM(ProcessArea)) <> '' {whereClause};

                SELECT DISTINCT Shift FROM ProductionOrders 
                WHERE Shift IS NOT NULL AND LTRIM(RTRIM(Shift)) <> '' {whereClause};

                SELECT DISTINCT ProductionOrderNumber
                FROM ProductionOrders
                WHERE ProductionOrderNumber IS NOT NULL AND LTRIM(RTRIM(ProductionOrderNumber)) <> '' {whereClause}
                ORDER BY ProductionOrderNumber DESC;";

            using var multi = await conn.QueryMultipleAsync(sql, p);
            var result = ApiResponse<OrderFiltersDto>.Success(new OrderFiltersDto
            {
                ProcessAreas = await multi.ReadAsync<string>(),
                Shifts = await multi.ReadAsync<string>(),
                ProductionOrderNumbers = await multi.ReadAsync<string>()
            });

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<OrderStatsDto>> GetStatsSearchAsync(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses)
        {
            using var conn = Connection;
            var where = new List<string>();
            var p = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                where.Add(@"(po.ProductionOrderNumber LIKE @q OR po.ProductCode LIKE @q OR po.ProductionLine LIKE @q OR po.RecipeCode LIKE @q)");
                p.Add("q", $"%{searchQuery.Trim()}%");
            }

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out DateTime df))
            {
                where.Add("po.PlannedStart >= @dateFrom");
                p.Add("dateFrom", df.Date);
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out DateTime dt))
            {
                where.Add("po.PlannedStart < DATEADD(DAY, 1, @dateTo)");
                p.Add("dateTo", dt.Date);
            }

            if (!string.IsNullOrWhiteSpace(processAreas))
            {
                var arr = processAreas.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                where.Add("po.ProcessArea IN @pa");
                p.Add("pa", arr);
            }

            if (!string.IsNullOrWhiteSpace(shifts))
            {
                var arr = shifts.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                where.Add("po.Shift IN @sh");
                p.Add("sh", arr);
            }

            string statusCondition = "";
            if (!string.IsNullOrWhiteSpace(statuses))
            {
                var arr = statuses.Split(',').Select(s => s.Trim()).ToList();
                var conds = new List<string>();
                if (arr.Contains("Đang chạy")) conds.Add("r.ProductionOrderNumber IS NOT NULL");
                if (arr.Contains("Đang chờ")) conds.Add("r.ProductionOrderNumber IS NULL");
                if (conds.Count == 1) statusCondition = conds[0];
                else if (conds.Count == 2) statusCondition = $"({string.Join(" OR ", conds)})";
            }

            var allConditions = where.ToList();
            if (!string.IsNullOrEmpty(statusCondition)) allConditions.Add(statusCondition);
            string whereClause = allConditions.Count > 0 ? "WHERE " + string.Join(" AND ", allConditions) : "";

            var sql = $@"
                SELECT
                    COUNT(*) AS total,
                    SUM(CASE WHEN po.Status = 2 THEN 1 ELSE 0 END) AS completed,
                    SUM(CASE WHEN mmc.ProductionOrderNumber IS NOT NULL THEN 1 ELSE 0 END) AS inProgress
                FROM ProductionOrders po
                LEFT JOIN (
                    SELECT DISTINCT ProductionOrderNumber FROM MESMaterialConsumption
                ) mmc ON po.ProductionOrderNumber = mmc.ProductionOrderNumber
                {whereClause}
            ";

            var stats = await conn.QueryFirstAsync(sql, p);
            int total = stats.total ?? 0;
            int inProgress = stats.inProgress ?? 0;
            int completed = stats.completed ?? 0;

            return ApiResponse<OrderStatsDto>.Success(new OrderStatsDto
            {
                Total = total,
                InProgress = inProgress,
                Completed = completed,
                Stopped = total - inProgress
            });
        }

        public async Task<ApiResponse<OrderStatsDto>> GetStatsSearchV2Async(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses, string pos, string batchIds)
        {
            using var conn = Connection;
            var where = new List<string>();
            var p = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                where.Add(@"(po.ProductionOrderNumber LIKE @search OR po.ProductCode LIKE @search OR po.ProductionLine LIKE @search OR po.RecipeCode LIKE @search)");
                p.Add("search", $"%{searchQuery.Trim()}%");
            }

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df))
            {
                where.Add("po.PlannedStart >= @dateFrom");
                p.Add("dateFrom", df.Date);
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt))
            {
                where.Add("po.PlannedStart < DATEADD(DAY, 1, @dateTo)");
                p.Add("dateTo", dt.Date);
            }

            if (!string.IsNullOrWhiteSpace(processAreas))
            {
                var arr = processAreas.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("po.ProcessArea IN @pa"); p.Add("pa", arr); }
            }

            if (!string.IsNullOrWhiteSpace(shifts))
            {
                var arr = shifts.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("po.Shift IN @sh"); p.Add("sh", arr); }
            }

            if (!string.IsNullOrWhiteSpace(pos))
            {
                where.Add("po.ProductionOrderNumber LIKE @poSearch");
                p.Add("poSearch", $"%{pos.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(batchIds))
            {
                var arr = batchIds.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("b.BatchId IN @batchIds"); p.Add("batchIds", arr); }
            }

            string statusCondition = "";
            if (!string.IsNullOrWhiteSpace(statuses))
            {
                var arr = statuses.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                var conds = new List<string>();
                if (arr.Contains("Bình thường")) conds.Add("po.Status = 1");
                if (arr.Contains("Đã hủy")) conds.Add("po.Status = -1");
                if (conds.Count == 1) statusCondition = conds[0];
                else if (conds.Count > 1) statusCondition = "(" + string.Join(" OR ", conds) + ")";
            }

            var allConditions = where.ToList();
            if (!string.IsNullOrEmpty(statusCondition)) allConditions.Add(statusCondition);
            string whereClause = allConditions.Count > 0 ? "WHERE " + string.Join(" AND ", allConditions) : "";

            var sql = $@"
                WITH FilteredPO AS (
                    SELECT DISTINCT po.ProductionOrderNumber, po.Status
                    FROM ProductionOrders po
                    LEFT JOIN Batches b ON b.ProductionOrderId = po.ProductionOrderId
                    {whereClause}
                )
                SELECT
                    COUNT(*) AS total,
                    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS completed,
                    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS inProgress,
                    SUM(CASE WHEN Status = -1 THEN 1 ELSE 0 END) AS stopped
                FROM FilteredPO
            ";

            var stats = await conn.QueryFirstAsync(sql, p);
            return ApiResponse<OrderStatsDto>.Success(new OrderStatsDto
            {
                Total = (int)(stats.total ?? 0),
                InProgress = (int)(stats.inProgress ?? 0),
                Completed = (int)(stats.completed ?? 0),
                Stopped = (int)(stats.stopped ?? 0)
            });
        }

        public async Task<ApiResponse<PagedResponse<ProductionOrderDto>>> SearchAsync(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, int page, int limit, int total)
        {
            using var conn = Connection;
            page = Math.Max(1, page);
            limit = Math.Min(100, Math.Max(1, limit));
            int offset = (page - 1) * limit;

            var where = new List<string>();
            var p = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                where.Add(@"(po.ProductionOrderNumber LIKE @q OR po.ProductCode LIKE @q OR po.ProductionLine LIKE @q OR po.RecipeCode LIKE @q)");
                p.Add("q", $"%{searchQuery.Trim()}%");
            }

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df))
            {
                where.Add("po.PlannedStart >= @dateFrom");
                p.Add("dateFrom", df.Date);
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt))
            {
                where.Add("po.PlannedStart < DATEADD(DAY, 1, @dateTo)");
                p.Add("dateTo", dt.Date);
            }

            if (!string.IsNullOrWhiteSpace(processAreas))
            {
                var arr = processAreas.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("po.ProcessArea IN @pa"); p.Add("pa", arr); }
            }

            if (!string.IsNullOrWhiteSpace(shifts))
            {
                var arr = shifts.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("po.Shift IN @sh"); p.Add("sh", arr); }
            }

            string statusCondition = "";
            if (!string.IsNullOrWhiteSpace(statuses))
            {
                var arr = statuses.Split(',').Select(s => s.Trim()).ToList();
                var conds = new List<string>();
                if (arr.Contains("Đang chạy")) conds.Add("mmc.ProductionOrderNumber IS NOT NULL");
                if (arr.Contains("Đang chờ")) conds.Add("mmc.ProductionOrderNumber IS NULL");
                if (conds.Count == 1) statusCondition = conds[0];
                else if (conds.Count == 2) statusCondition = "(" + string.Join(" OR ", conds) + ")";
            }

            if (!string.IsNullOrEmpty(statusCondition)) where.Add(statusCondition);
            string whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            if (page == 1 || total == 0)
            {
                var countSql = $@"
                    SELECT COUNT(*) AS total
                    FROM ProductionOrders po
                    LEFT JOIN (
                      SELECT DISTINCT ProductionOrderNumber
                      FROM MESMaterialConsumption
                    ) mmc
                      ON po.ProductionOrderNumber = mmc.ProductionOrderNumber
                    {whereClause}
                ";
                total = await conn.ExecuteScalarAsync<int>(countSql, p);
            }

            p.Add("offset", offset);
            p.Add("limit", limit);

            var sqlQuery = $@"
                SELECT
                    po.ProductionOrderId, po.ProductionOrderNumber, po.ProductionLine, po.ProductCode, po.RecipeCode,
                    po.RecipeVersion, po.LotNumber, po.ProcessArea, po.PlannedStart, po.PlannedEnd, po.Quantity,
                    po.UnitOfMeasurement, po.Plant, po.Shopfloor, po.Shift,
                    pm.ItemName, rd.RecipeName,
                    CASE WHEN mmc.ProductionOrderNumber IS NOT NULL THEN 1 ELSE 0 END AS Status,
                    mmc.MaxBatch AS CurrentBatch,
                    ISNULL(b.TotalBatches, 0) AS TotalBatches
                FROM ProductionOrders po
                LEFT JOIN ProductMasters pm ON po.ProductCode = pm.ItemCode
                LEFT JOIN RecipeDetails rd ON po.RecipeCode = rd.RecipeCode AND po.RecipeVersion = rd.Version
                LEFT JOIN (
                    SELECT ProductionOrderNumber, MAX(BatchCode) AS MaxBatch
                    FROM MESMaterialConsumption
                    GROUP BY ProductionOrderNumber
                ) mmc ON po.ProductionOrderNumber = mmc.ProductionOrderNumber
                LEFT JOIN (
                    SELECT ProductionOrderId, COUNT(*) AS TotalBatches
                    FROM Batches
                    GROUP BY ProductionOrderId
                ) b ON po.ProductionOrderId = b.ProductionOrderId
                {whereClause}
                ORDER BY po.ProductionOrderId DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
            ";

            p.Add("offset", offset);
            p.Add("limit", limit);

            var dtos = await conn.QueryAsync<ProductionOrderDto>(sqlQuery, p);

            return ApiResponse<PagedResponse<ProductionOrderDto>>.Success(
                new PagedResponse<ProductionOrderDto>(dtos, total, page, limit)
            );
        }

        public async Task<ApiResponse<PagedResponse<ProductionOrderDto>>> SearchV2Async(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, string? pos, string? batchIds, int page, int limit, int total)
        {
            using var conn = Connection;
            page = Math.Max(1, page);
            limit = Math.Min(100, Math.Max(1, limit));
            int offset = (page - 1) * limit;

            var where = new List<string>();
            var p = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                where.Add(@"(po.ProductionOrderNumber LIKE @q OR po.ProductCode LIKE @q OR po.ProductionLine LIKE @q OR po.RecipeCode LIKE @q)");
                p.Add("q", $"%{searchQuery.Trim()}%");
            }

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var df))
            {
                where.Add("po.PlannedStart >= @dateFrom");
                p.Add("dateFrom", df.Date);
            }

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var dt))
            {
                where.Add("po.PlannedStart < DATEADD(DAY, 1, @dateTo)");
                p.Add("dateTo", dt.Date);
            }

            if (!string.IsNullOrWhiteSpace(processAreas))
            {
                var arr = processAreas.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("po.ProcessArea IN @pa"); p.Add("pa", arr); }
            }

            if (!string.IsNullOrWhiteSpace(shifts))
            {
                var arr = shifts.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("po.Shift IN @sh"); p.Add("sh", arr); }
            }

            if (!string.IsNullOrWhiteSpace(pos))
            {
                where.Add("po.ProductionOrderNumber LIKE @poSearch");
                p.Add("poSearch", $"%{pos.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(batchIds))
            {
                var arr = batchIds.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (arr.Length > 0) { where.Add("EXISTS (SELECT 1 FROM Batches b WHERE b.ProductionOrderId = po.ProductionOrderId AND b.BatchId IN @batchIds)"); p.Add("batchIds", arr); }
            }

            string statusCondition = "";
            if (!string.IsNullOrWhiteSpace(statuses))
            {
                var arr = statuses.Split(',').Select(x => x.Trim()).ToList();
                var conds = new List<string>();
                if (arr.Contains("Bình thường")) conds.Add("po.Status = 1");
                if (arr.Contains("Đã hủy")) conds.Add("po.Status = -1");
                if (conds.Count == 1) statusCondition = conds[0];
                else if (conds.Count > 1) statusCondition = "(" + string.Join(" OR ", conds) + ")";
            }

            if (!string.IsNullOrEmpty(statusCondition)) where.Add(statusCondition);
            string whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            if (page == 1 || total == 0)
            {
                var countSql = $@"
                    SELECT COUNT(*) AS total
                    FROM ProductionOrders po
                    LEFT JOIN (
                      SELECT DISTINCT ProductionOrderNumber
                      FROM MESMaterialConsumption
                    ) mmc
                      ON po.ProductionOrderNumber = mmc.ProductionOrderNumber
                    {whereClause}
                ";
                total = await conn.ExecuteScalarAsync<int>(countSql, p);
            }

            p.Add("offset", offset);
            p.Add("limit", limit);

            var sqlQuery = $@"
                SELECT
                    po.ProductionOrderId, po.ProductionOrderNumber, po.ProductionLine, po.ProductCode, po.RecipeCode, 
                    po.RecipeVersion, po.LotNumber, po.ProcessArea, po.PlannedStart, po.PlannedEnd, po.Quantity, 
                    po.UnitOfMeasurement, po.Plant, po.Shopfloor, po.Shift, po.Status,
                    pm.ItemName, rd.RecipeName,
                    mmc.MaxBatch AS CurrentBatch, 
                    ISNULL(b_cnt.TotalBatches, 0) AS TotalBatches
                FROM ProductionOrders po
                LEFT JOIN ProductMasters pm ON po.ProductCode = pm.ItemCode
                LEFT JOIN RecipeDetails rd ON po.RecipeCode = rd.RecipeCode AND po.RecipeVersion = rd.Version
                LEFT JOIN (
                    SELECT ProductionOrderNumber, MAX(BatchCode) AS MaxBatch 
                    FROM MESMaterialConsumption 
                    GROUP BY ProductionOrderNumber
                ) mmc ON po.ProductionOrderNumber = mmc.ProductionOrderNumber
                LEFT JOIN (
                    SELECT ProductionOrderId, COUNT(*) AS TotalBatches 
                    FROM Batches 
                    GROUP BY ProductionOrderId
                ) b_cnt ON po.ProductionOrderId = b_cnt.ProductionOrderId
                {whereClause}
                ORDER BY po.ProductionOrderId DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

            var rows = (await conn.QueryAsync<ProductionOrderDto>(sqlQuery, p)).ToList();

            // Fetch batches for each ProductionOrderId
            var poIds = rows.Select(r => r.ProductionOrderId).Distinct().ToList();
            if (poIds.Any())
            {
                var batchesSql = "SELECT BatchId, ProductionOrderId, BatchNumber, Quantity, UnitOfMeasurement, Status FROM Batches WHERE ProductionOrderId IN @poIds";
                var batchesResult = await conn.QueryAsync<BatchDto>(batchesSql, new { poIds });
                var batchesByPoId = batchesResult.GroupBy(b => b.ProductionOrderId).ToDictionary(g => g.Key, g => g.ToList());
                
                foreach (var po in rows)
                {
                    if (batchesByPoId.TryGetValue(po.ProductionOrderId, out var batches))
                    {
                        po.Batches = batches;
                    }
                }
            }

            return ApiResponse<PagedResponse<ProductionOrderDto>>.Success(
                new PagedResponse<ProductionOrderDto>(rows, total, page, limit)
            );
        }
    }
}
