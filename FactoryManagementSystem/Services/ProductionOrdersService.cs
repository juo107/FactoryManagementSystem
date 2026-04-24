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

                SELECT DISTINCT CAST(Status AS NVARCHAR) FROM ProductionOrders 
                WHERE Status IS NOT NULL {whereClause};
            ";

            using var multi = await conn.QueryMultipleAsync(sql, p);
            var result = ApiResponse<OrderFiltersDto>.Success(new OrderFiltersDto
            {
                ProcessAreas = await multi.ReadAsync<string>(),
                Shifts = await multi.ReadAsync<string>(),
                Statuses = await multi.ReadAsync<string>()
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

                SELECT DISTINCT CAST(Status AS NVARCHAR) FROM ProductionOrders 
                WHERE Status IS NOT NULL {whereClause};

                SELECT DISTINCT ProductionOrderNumber
                FROM ProductionOrders
                WHERE ProductionOrderNumber IS NOT NULL AND LTRIM(RTRIM(ProductionOrderNumber)) <> '' {whereClause}
                ORDER BY ProductionOrderNumber DESC;";

            using var multi = await conn.QueryMultipleAsync(sql, p);
            var result = ApiResponse<OrderFiltersDto>.Success(new OrderFiltersDto
            {
                ProcessAreas = await multi.ReadAsync<string>(),
                Shifts = await multi.ReadAsync<string>(),
                Statuses = await multi.ReadAsync<string>(),
                ProductionOrderNumbers = await multi.ReadAsync<string>()
            });

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<OrderStatsDto>> GetStatsSearchAsync(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses)
        {
            string queryParams = $"q={searchQuery}_df={dateFrom}_dt={dateTo}_pa={processAreas}_sh={shifts}_st={statuses}";
            string cacheKey = $"production_orders:stats:{queryParams}";
            var cached = await _cache.GetAsync<ApiResponse<OrderStatsDto>>(cacheKey);
            if (cached != null) return cached;

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
                var statusStr = statuses.Trim('[', ']').Replace("\"", "");
                var arr = statusStr.Split(',').Select(s => s.Trim()).ToList();
                var conds = new List<string>();
                if (arr.Contains("Bình thường") || arr.Contains("1")) 
                    conds.Add("po.Status = 1");
                if (arr.Contains("Đã hủy") || arr.Contains("-1")) 
                    conds.Add("po.Status = -1");
                
                if (conds.Count > 0)
                {
                    statusCondition = conds.Count == 1 ? conds[0] : "(" + string.Join(" OR ", conds) + ")";
                }
            }

            var allConditions = where.ToList();
            if (!string.IsNullOrEmpty(statusCondition)) allConditions.Add(statusCondition);
            string whereClause = allConditions.Count > 0 ? "WHERE " + string.Join(" AND ", allConditions) : "";

            var sql = $@"
                SELECT
                    COUNT(*) AS total,
                    SUM(CASE WHEN po.Status = 1 THEN 1 ELSE 0 END) AS inProgress,
                    SUM(CASE WHEN po.Status = -1 THEN 1 ELSE 0 END) AS stopped,
                    SUM(CASE WHEN po.Status = 2 THEN 1 ELSE 0 END) AS completed
                FROM ProductionOrders po
                LEFT JOIN (
                    SELECT DISTINCT ProductionOrderNumber FROM MESMaterialConsumption
                ) mmc ON LTRIM(RTRIM(po.ProductionOrderNumber)) = LTRIM(RTRIM(mmc.ProductionOrderNumber))
                {whereClause}
            ";

            var stats = await conn.QueryFirstAsync(sql, p);
            int totalCount = stats.total ?? 0;
            int inProgressCount = stats.inProgress ?? 0;
            int stoppedCount = stats.stopped ?? 0;
            int completedCount = stats.completed ?? 0;

            var result = ApiResponse<OrderStatsDto>.Success(new OrderStatsDto
            {
                Total = totalCount,
                InProgress = inProgressCount,
                Completed = completedCount,
                Stopped = stoppedCount
            });
            
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<OrderStatsDto>> GetStatsSearchV2Async(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses, string pos, string batchIds)
        {
            string queryParams = $"q={searchQuery}_df={dateFrom}_dt={dateTo}_pa={processAreas}_sh={shifts}_st={statuses}_po={pos}_ba={batchIds}";
            string cacheKey = $"production_orders:stats_v2:{queryParams}";
            var cached = await _cache.GetAsync<ApiResponse<OrderStatsDto>>(cacheKey);
            if (cached != null) return cached;

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
                var statusStr = statuses.Trim();
                if (statusStr.StartsWith("[") && statusStr.EndsWith("]"))
                {
                    statusStr = statusStr.Substring(1, statusStr.Length - 2);
                }
                statusStr = statusStr.Replace("\"", "").Replace("'", "");
                var arr = statusStr.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var conds = new List<string>();
                if (arr.Contains("Đang chạy") || arr.Contains("1")) 
                    conds.Add("mmc.ProductionOrderNumber IS NOT NULL");
                if (arr.Contains("Đang chờ") || arr.Contains("0")) 
                    conds.Add("mmc.ProductionOrderNumber IS NULL");
                
                if (conds.Count > 0)
                {
                    statusCondition = conds.Count == 1 ? conds[0] : "(" + string.Join(" OR ", conds) + ")";
                }
            }

            var allConditions = where.ToList();
            if (!string.IsNullOrEmpty(statusCondition)) allConditions.Add(statusCondition);
            string whereClause = allConditions.Count > 0 ? "WHERE " + string.Join(" AND ", allConditions) : "";

            var sql = $@"
                WITH FilteredPO AS (
                    SELECT DISTINCT po.ProductionOrderNumber
                    FROM ProductionOrders po
                    LEFT JOIN Batches b ON b.ProductionOrderId = po.ProductionOrderId
                    LEFT JOIN (
                        SELECT DISTINCT ProductionOrderNumber FROM MESMaterialConsumption
                    ) mmc ON LTRIM(RTRIM(po.ProductionOrderNumber)) = LTRIM(RTRIM(mmc.ProductionOrderNumber))
                    {whereClause}
                )
                SELECT
                    COUNT(*) AS total,
                    SUM(CASE WHEN mmc.ProductionOrderNumber IS NOT NULL THEN 1 ELSE 0 END) AS inProgress,
                    SUM(CASE WHEN mmc.ProductionOrderNumber IS NULL THEN 1 ELSE 0 END) AS stopped,
                    SUM(CASE WHEN po.Status = 2 THEN 1 ELSE 0 END) AS completed
                FROM FilteredPO po_filtered
                JOIN ProductionOrders po ON po.ProductionOrderNumber = po_filtered.ProductionOrderNumber
                LEFT JOIN (
                    SELECT DISTINCT LTRIM(RTRIM(ProductionOrderNumber)) AS ProductionOrderNumber FROM MESMaterialConsumption
                ) mmc ON LTRIM(RTRIM(po.ProductionOrderNumber)) = mmc.ProductionOrderNumber
            ";

            var stats = await conn.QueryFirstAsync(sql, p);
            var result = ApiResponse<OrderStatsDto>.Success(new OrderStatsDto
            {
                Total = (int)(stats.total ?? 0),
                InProgress = (int)(stats.inProgress ?? 0),
                Completed = (int)(stats.completed ?? 0),
                Stopped = (int)(stats.stopped ?? 0)
            });
            
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<PagedResponse<ProductionOrderDto>>> SearchAsync(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, int page, int limit, int total)
        {
            string queryParams = $"q={searchQuery}_df={dateFrom}_dt={dateTo}_pa={processAreas}_sh={shifts}_st={statuses}";
            string cacheKey = $"production_orders:search:{queryParams}:{page}:{limit}:{total}";
            var cached = await _cache.GetAsync<ApiResponse<PagedResponse<ProductionOrderDto>>>(cacheKey);
            if (cached != null) return cached;

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
                var statusStr = statuses.Trim('[', ']').Replace("\"", "");
                var arr = statusStr.Split(',').Select(s => s.Trim()).ToList();
                var conds = new List<string>();
                if (arr.Contains("Bình thường") || arr.Contains("1")) 
                    conds.Add("po.Status = 1");
                if (arr.Contains("Đã hủy") || arr.Contains("-1")) 
                    conds.Add("po.Status = -1");
                
                if (conds.Count == 1) statusCondition = conds[0];
                else if (conds.Count == 2) statusCondition = $"({string.Join(" OR ", conds)})";
            }

            if (!string.IsNullOrEmpty(statusCondition)) where.Add(statusCondition);
            string whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            int finalTotal = total;
            if (page == 1 || finalTotal == 0)
            {
                var countSql = $@"
                    SELECT COUNT(*) AS total
                    FROM ProductionOrders po
                    LEFT JOIN (
                      SELECT DISTINCT LTRIM(RTRIM(ProductionOrderNumber)) AS ProductionOrderNumber
                      FROM MESMaterialConsumption
                    ) mmc
                      ON LTRIM(RTRIM(po.ProductionOrderNumber)) = mmc.ProductionOrderNumber
                    {whereClause}
                ";
                finalTotal = await conn.ExecuteScalarAsync<int>(countSql, p);
            }

            p.Add("offset", offset);
            p.Add("limit", limit);

            var sqlQuery = $@"
                SELECT
                    po.ProductionOrderId, po.ProductionOrderNumber, po.ProductionLine, po.ProductCode, po.RecipeCode,
                    po.RecipeVersion, po.LotNumber, po.ProcessArea, po.PlannedStart, po.PlannedEnd, 
                    CAST(CAST(po.Quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity,
                    po.UnitOfMeasurement, po.Plant, po.Shopfloor, po.Shift, rd.RecipeDetailsId,
                    pm.ItemName AS ProductName, rd_info.RecipeName, 
                    CAST(CAST(p_prod.PlanQuantity AS FLOAT) AS DECIMAL(18,4)) AS ProductQuantity,
                    po.Status AS Status,
                    mmc.MaxBatch AS CurrentBatch,
                    ISNULL(b.TotalBatches, 0) AS TotalBatches
                FROM ProductionOrders po
                LEFT JOIN (
                    SELECT ItemCode, MAX(ItemName) AS ItemName
                    FROM ProductMasters
                    GROUP BY ItemCode
                ) pm ON po.ProductCode = pm.ItemCode
                LEFT JOIN (
                    SELECT ProductCode, Version, MAX(RecipeDetailsId) as RecipeDetailsId
                    FROM RecipeDetails
                    GROUP BY ProductCode, Version
                ) rd ON rd.ProductCode = po.ProductCode AND CAST(rd.Version AS NVARCHAR) = CAST(po.RecipeVersion AS NVARCHAR)
                LEFT JOIN RecipeDetails rd_info ON rd_info.RecipeDetailsId = rd.RecipeDetailsId
                LEFT JOIN (
                    SELECT ProductCode, MAX(PlanQuantity) AS PlanQuantity
                    FROM Products
                    GROUP BY ProductCode
                ) p_prod ON po.ProductCode = p_prod.ProductCode
                -- Re-add the join to get the RecipeName if needed, or use rd_info
                -- Actually, let's just use rd_info for RecipeName
                
                LEFT JOIN (
                    SELECT LTRIM(RTRIM(ProductionOrderNumber)) AS ProductionOrderNumber, MAX(BatchCode) AS MaxBatch
                    FROM MESMaterialConsumption
                    GROUP BY LTRIM(RTRIM(ProductionOrderNumber))
                ) mmc ON LTRIM(RTRIM(po.ProductionOrderNumber)) = mmc.ProductionOrderNumber
                LEFT JOIN (
                    SELECT ProductionOrderId, COUNT(*) AS TotalBatches
                    FROM Batches
                    GROUP BY ProductionOrderId
                ) b ON po.ProductionOrderId = b.ProductionOrderId
                {whereClause}
                ORDER BY po.ProductionOrderId DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
            ";

            var dtos = await conn.QueryAsync<ProductionOrderDto>(sqlQuery, p);

            var result = ApiResponse<PagedResponse<ProductionOrderDto>>.Success(
                new PagedResponse<ProductionOrderDto>(dtos, finalTotal, page, limit)
            );
            
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<PagedResponse<ProductionOrderDto>>> SearchV2Async(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, string? pos, string? batchIds, int page, int limit, int total)
        {
            string queryParams = $"q={searchQuery}_df={dateFrom}_dt={dateTo}_pa={processAreas}_sh={shifts}_st={statuses}_po={pos}_ba={batchIds}";
            string cacheKey = $"production_orders:search_v2:{queryParams}:{page}:{limit}:{total}";
            var cached = await _cache.GetAsync<ApiResponse<PagedResponse<ProductionOrderDto>>>(cacheKey);
            if (cached != null) return cached;

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
                // Handle both comma-separated and JSON array formats
                var statusStr = statuses.Trim();
                if (statusStr.StartsWith("[") && statusStr.EndsWith("]"))
                {
                    statusStr = statusStr.Substring(1, statusStr.Length - 2);
                }
                statusStr = statusStr.Replace("\"", "").Replace("'", "");
                var arr = statusStr.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var conds = new List<string>();
                if (arr.Contains("Đang chạy") || arr.Contains("1")) 
                    conds.Add("mmc.ProductionOrderNumber IS NOT NULL");
                if (arr.Contains("Đang chờ") || arr.Contains("0")) 
                    conds.Add("mmc.ProductionOrderNumber IS NULL");
                
                if (conds.Count > 0)
                {
                    statusCondition = conds.Count == 1 ? conds[0] : "(" + string.Join(" OR ", conds) + ")";
                }
            }

            if (!string.IsNullOrEmpty(statusCondition)) where.Add(statusCondition);
            string whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            int finalTotal = total;
            if (page == 1 || finalTotal == 0)
            {
                var countSql = $@"
                    SELECT COUNT(*) AS total
                    FROM ProductionOrders po
                    LEFT JOIN (
                      SELECT DISTINCT LTRIM(RTRIM(ProductionOrderNumber)) AS ProductionOrderNumber
                      FROM MESMaterialConsumption
                    ) mmc
                      ON LTRIM(RTRIM(po.ProductionOrderNumber)) = mmc.ProductionOrderNumber
                    {whereClause}
                ";
                finalTotal = await conn.ExecuteScalarAsync<int>(countSql, p);
            }

            p.Add("offset", offset);
            p.Add("limit", limit);

            var sqlQuery = $@"
                SELECT
                    po.ProductionOrderId, po.ProductionOrderNumber, po.ProductionLine, po.ProductCode, po.RecipeCode, 
                    po.RecipeVersion, po.LotNumber, po.ProcessArea, po.PlannedStart, po.PlannedEnd, 
                    CAST(CAST(po.Quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity, 
                    po.UnitOfMeasurement, po.Plant, po.Shopfloor, po.Shift, rd.RecipeDetailsId,
                    pm.ItemName AS ProductName, rd_info.RecipeName, 
                    CAST(CAST(p_prod.PlanQuantity AS FLOAT) AS DECIMAL(18,4)) AS ProductQuantity,
                    CASE WHEN mmc.ProductionOrderNumber IS NOT NULL THEN 1 ELSE 0 END AS Status,
                    mmc.MaxBatch AS CurrentBatch, 
                    ISNULL(b_cnt.TotalBatches, 0) AS TotalBatches
                FROM ProductionOrders po
                LEFT JOIN (
                    SELECT ItemCode, MAX(ItemName) AS ItemName
                    FROM ProductMasters
                    GROUP BY ItemCode
                ) pm ON po.ProductCode = pm.ItemCode
                LEFT JOIN (
                    SELECT ProductCode, Version, MAX(RecipeDetailsId) as RecipeDetailsId
                    FROM RecipeDetails
                    GROUP BY ProductCode, Version
                ) rd ON rd.ProductCode = po.ProductCode AND CAST(rd.Version AS NVARCHAR) = CAST(po.RecipeVersion AS NVARCHAR)
                LEFT JOIN RecipeDetails rd_info ON rd_info.RecipeDetailsId = rd.RecipeDetailsId
                LEFT JOIN (
                    SELECT ProductCode, MAX(PlanQuantity) AS PlanQuantity
                    FROM Products
                    GROUP BY ProductCode
                ) p_prod ON po.ProductCode = p_prod.ProductCode
                LEFT JOIN (
                    SELECT LTRIM(RTRIM(ProductionOrderNumber)) AS ProductionOrderNumber, MAX(BatchCode) AS MaxBatch 
                    FROM MESMaterialConsumption 
                    GROUP BY LTRIM(RTRIM(ProductionOrderNumber))
                ) mmc ON LTRIM(RTRIM(po.ProductionOrderNumber)) = mmc.ProductionOrderNumber
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
                var batchesSql = "SELECT BatchId, ProductionOrderId, BatchNumber, CAST(CAST(Quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity, UnitOfMeasurement, Status FROM Batches WHERE ProductionOrderId IN @poIds";
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

            var result = ApiResponse<PagedResponse<ProductionOrderDto>>.Success(
                new PagedResponse<ProductionOrderDto>(rows, finalTotal, page, limit)
            );
            
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
    }
}
