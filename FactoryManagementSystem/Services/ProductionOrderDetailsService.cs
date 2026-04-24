using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.ProductionOrders;
using FactoryManagementSystem.DTOs.Materials;
using FactoryManagementSystem.DTOs.Recipes;

namespace FactoryManagementSystem.Services
{
    public class ProductionOrderDetailsService : IProductionOrderDetailsService
    {
        private readonly IConfiguration _config;
        private readonly IRedisCacheService _cache;

        public ProductionOrderDetailsService(IConfiguration config, IRedisCacheService cache)
        {
            _config = config;
            _cache = cache;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<ApiResponse<IEnumerable<BatchDto>>> GetBatchesAsync(int productionOrderId)
        {
            string cacheKey = $"production_details:batches:{productionOrderId}";
            var cached = await _cache.GetAsync<ApiResponse<IEnumerable<BatchDto>>>(cacheKey);
            if (cached != null) return cached;

            var sql = @"SELECT * FROM Batches WHERE ProductionOrderId = @ProductionOrderId";
            using var conn = Connection;
            var data = await conn.QueryAsync<BatchDto>(sql, new { ProductionOrderId = productionOrderId });
            var result = ApiResponse<IEnumerable<BatchDto>>.Success(data);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<object>> GetIngredientsByProductAsync(string productionOrderNumber)
        {
            var sql = @"
              SELECT
                i.IngredientCode AS ingredientCode,
                CAST(CAST(i.Quantity AS FLOAT) AS DECIMAL(18,4)) AS quantity,
                i.UnitOfMeasurement AS unitOfMeasurement,
                pm.ItemName AS itemName,
                po.ProductCode AS productCode,
                po.RecipeVersion AS recipeVersion
              FROM ProductionOrders po
              JOIN (
                SELECT ProductCode, Version, MAX(RecipeDetailsId) as LatestId
                FROM RecipeDetails
                GROUP BY ProductCode, Version
              ) rd_latest ON rd_latest.ProductCode = po.ProductCode
                AND CAST(rd_latest.Version AS NVARCHAR) = CAST(po.RecipeVersion AS NVARCHAR)
              JOIN Processes p 
                ON p.RecipeDetailsId = rd_latest.LatestId
              JOIN Ingredients i 
                ON i.ProcessId = p.ProcessId
              LEFT JOIN ProductMasters pm 
                ON pm.ItemCode = i.IngredientCode
              WHERE po.ProductionOrderNumber = @prodOrderNum
            ";

            using var conn = Connection;
            var rows = (await conn.QueryAsync(sql, new { prodOrderNum = productionOrderNumber.Trim() })).ToList();

            return ApiResponse<object>.Success(new
            {
                productCode = rows.Any() ? rows[0].ProductCode : null,
                recipeVersion = rows.Any() ? rows[0].RecipeVersion : null,
                total = rows.Count,
                data = rows
            });
        }

        public async Task<ApiResponse<object>> GetMaterialConsumptionsAsync(string productionOrderNumber, int page, int limit, List<string>? batches = null)
        {
            var pageNum = Math.Max(1, page);
            var pageLimit = Math.Min(1000, Math.Max(1, limit));
            var from = (pageNum - 1) * pageLimit + 1;
            var to = pageNum * pageLimit;

            var sql = @"
                ;WITH AllBatches AS (
                  SELECT b.BatchNumber AS batchCode
                  FROM Batches b
                  JOIN ProductionOrders po ON po.ProductionOrderId = b.ProductionOrderId
                  WHERE po.ProductionOrderNumber = @prodOrderNum
                    AND (@hasBatches = 0 OR b.BatchNumber IN @batchList)
                  
                  UNION
                  
                  SELECT mc.batchCode
                  FROM MESMaterialConsumption mc
                  WHERE mc.productionOrderNumber = @prodOrderNum
                    AND (@hasBatches = 0 OR mc.batchCode IN @batchList)
                ),
                BatchCTE AS (
                  SELECT batchCode, ROW_NUMBER() OVER (ORDER BY batchCode) AS rn
                  FROM AllBatches
                ),
                PagedBatch AS (
                  SELECT batchCode
                  FROM BatchCTE
                  WHERE rn BETWEEN @from AND @to
                ),
                RecipeIngredient AS (
                  SELECT 
                    i.IngredientCode,
                    pm.ItemName,
                    CAST(CAST(i.Quantity AS FLOAT) AS DECIMAL(18,4)) AS quantity,
                    i.UnitOfMeasurement
                  FROM ProductionOrders po
                  JOIN (
                    SELECT ProductCode, Version, MAX(RecipeDetailsId) as LatestId
                    FROM RecipeDetails
                    GROUP BY ProductCode, Version
                  ) rd_latest ON rd_latest.ProductCode = po.ProductCode
                    AND CAST(rd_latest.Version AS NVARCHAR) = CAST(po.RecipeVersion AS NVARCHAR)
                  JOIN Processes p ON p.RecipeDetailsId = rd_latest.LatestId
                  JOIN Ingredients i ON i.ProcessId = p.ProcessId
                  LEFT JOIN ProductMasters pm ON pm.ItemCode = i.IngredientCode
                  WHERE po.ProductionOrderNumber = @prodOrderNum
                ),
                ExtraIngredient AS (
                  SELECT DISTINCT
                    mc.ingredientCode AS IngredientCode,
                    pm.ItemName
                  FROM MESMaterialConsumption mc
                  JOIN PagedBatch pb
                    ON pb.batchCode = mc.batchCode
                  LEFT JOIN RecipeIngredient r
                    ON r.IngredientCode = mc.ingredientCode
                  LEFT JOIN ProductMasters pm
                    ON pm.ItemCode = mc.ingredientCode
                  WHERE mc.productionOrderNumber = @prodOrderNum
                    AND r.IngredientCode IS NULL
                )

                SELECT
                  pb.batchCode AS BatchCode,
                  CASE 
                    WHEN r.ItemName IS NOT NULL THEN r.IngredientCode + ' - ' + r.ItemName
                    ELSE r.IngredientCode
                  END AS IngredientCode,
                  r.ItemName AS IngredientName,
                  mc.id AS Id,
                  mc.lot AS Lot,
                  CAST(CAST(mc.quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity,
                  COALESCE(mc.unitOfMeasurement, ing.UnitOfMeasurement) AS UnitOfMeasurement,
                  mc.datetime AS Datetime,
                  mc.operator_ID AS Operator_ID,
                  mc.supplyMachine AS SupplyMachine,
                  mc.count AS Count,
                  mc.request AS Request,
                  mc.respone AS Respone,
                  mc.status1 AS Status1,
                  mc.timestamp AS Timestamp
                FROM PagedBatch pb
                CROSS JOIN RecipeIngredient r
                LEFT JOIN MESMaterialConsumption mc
                  ON mc.productionOrderNumber = @prodOrderNum
                  AND mc.batchCode = pb.batchCode
                  AND mc.ingredientCode = r.IngredientCode
                LEFT JOIN Ingredients ing
                  ON ing.IngredientCode = r.IngredientCode
                WHERE (ISNULL(TRY_CAST(r.quantity AS FLOAT), 0) > 0 OR mc.id IS NOT NULL)

                UNION ALL

                SELECT
                  mc.batchCode AS BatchCode,
                  CASE 
                    WHEN e.ItemName IS NOT NULL THEN e.IngredientCode + ' - ' + e.ItemName
                    ELSE e.IngredientCode
                  END AS IngredientCode,
                  e.ItemName AS IngredientName,
                  mc.id AS Id,
                  mc.lot AS Lot,
                  CAST(CAST(mc.quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity,
                  mc.unitOfMeasurement AS UnitOfMeasurement,
                  mc.datetime AS Datetime,
                  mc.operator_ID AS Operator_ID,
                  mc.supplyMachine AS SupplyMachine,
                  mc.count AS Count,
                  mc.request AS Request,
                  mc.respone AS Respone,
                  mc.status1 AS Status1,
                  mc.timestamp AS Timestamp
                FROM MESMaterialConsumption mc
                JOIN PagedBatch pb
                  ON pb.batchCode = mc.batchCode
                JOIN ExtraIngredient e
                  ON e.IngredientCode = mc.ingredientCode
                WHERE mc.productionOrderNumber = @prodOrderNum

                ORDER BY BatchCode, IngredientCode;
            ";

            using var conn = Connection;
            var data = (await conn.QueryAsync<MaterialConsumptionResponseDto>(sql, new 
            { 
                prodOrderNum = productionOrderNumber.Trim(), 
                from, 
                to,
                hasBatches = batches != null && batches.Any() ? 1 : 0,
                batchList = batches ?? new List<string>()
            })).ToList();

            return ApiResponse<object>.Success(new
            {
                page = pageNum,
                limit = pageLimit,
                data = data
            });
        }

        public async Task<ApiResponse<object>> GetMaterialConsumptionsExcludeBatchesAsync(
            string productionOrderNumber,
            int page,
            int limit,
        List<BatchFilterDto>? batchFilters)
        {
            var pageNum = Math.Max(1, page);
            var pageLimit = Math.Clamp(limit, 1, 1000);
            var offset = (pageNum - 1) * pageLimit;

            var p = new DynamicParameters();
            p.Add("prodOrderNum", productionOrderNumber.Trim());
            p.Add("offset", offset);
            p.Add("limit", pageLimit);

            // Lấy danh sách BatchCode từ DTO đầu vào
            var batchNumbers = batchFilters?
                .Select(b => b.BatchCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? new List<string>();

            // Xây dựng mệnh đề WHERE để tìm các tiêu thụ không thuộc danh sách Batch chính thức
            string whereClause = @"
        WHERE mc.ProductionOrderNumber = @prodOrderNum
          AND (
              mc.batchCode IS NULL 
              OR LTRIM(RTRIM(mc.batchCode)) = ''
              {0}
          )";

            if (batchNumbers.Any())
            {
                p.Add("batchNumbers", batchNumbers);
                whereClause = string.Format(whereClause, "OR mc.batchCode NOT IN @batchNumbers");
            }
            else
            {
                whereClause = string.Format(whereClause, "");
            }

            using var conn = Connection;

            // 1. Lấy tổng số lượng để phân trang
            var countSql = $"SELECT COUNT(1) FROM MESMaterialConsumption mc WITH (NOLOCK) {whereClause}";
            var totalCount = await conn.ExecuteScalarAsync<int>(countSql, p);

            if (totalCount == 0)
            {
                return ApiResponse<object>.Success(new
                {
                    page = pageNum,
                    limit = pageLimit,
                    totalCount = 0,
                    totalPages = 0,
                    data = new List<MaterialConsumptionResponseDto>()
                });
            }

            // 2. Lấy dữ liệu và Map thẳng vào DTO đầu ra
            var dataSql = $@"
        SELECT 
            mc.id AS Id, 
            mc.productionOrderNumber AS ProductionOrderNumber, 
            mc.batchCode AS BatchCode, 
            CASE 
                WHEN pm.ItemName IS NOT NULL THEN mc.ingredientCode + ' - ' + pm.ItemName
                ELSE mc.ingredientCode
            END AS IngredientCode,
            pm.ItemName AS IngredientName,
            mc.lot AS Lot, 
            CAST(CAST(mc.quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity, 
            mc.unitOfMeasurement AS UnitOfMeasurement,
            mc.datetime AS Datetime, 
            mc.operator_ID AS Operator_ID, 
            mc.supplyMachine AS SupplyMachine, 
            ISNULL(mc.count, 0) AS Count,
            mc.request AS Request, 
            mc.respone AS Respone, 
            mc.status1 AS Status1, 
            mc.timestamp AS Timestamp
        FROM MESMaterialConsumption mc WITH (NOLOCK)
        LEFT JOIN ProductMasters pm WITH (NOLOCK) ON pm.ItemCode = mc.ingredientCode
        {whereClause}
        ORDER BY mc.id DESC
        OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

            // Dapper tự động thực hiện việc đổ dữ liệu vào List DTO
            var items = (await conn.QueryAsync<MaterialConsumptionResponseDto>(dataSql, p)).ToList();

            return ApiResponse<object>.Success(new
            {
                page = pageNum,
                limit = pageLimit,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageLimit),
                data = items
            });
        }

        public async Task<ApiResponse<object>> GetBatchCodesWithMaterialsAsync(string productionOrderNumber)
        {
            string cacheKey = $"production_details:batch_codes:{productionOrderNumber}";
            var cached = await _cache.GetAsync<ApiResponse<object>>(cacheKey);
            if (cached != null) return cached;

            var sql = @"SELECT DISTINCT batchCode FROM MESMaterialConsumption WHERE ProductionOrderNumber = @productionOrderNumber ORDER BY batchCode ASC";
            using var conn = Connection;
            var rows = await conn.QueryAsync(sql, new { productionOrderNumber });
            
            var result = ApiResponse<object>.Success(rows.Select(r => new { BatchCode = r.batchCode }));
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }

        public async Task<ApiResponse<object>> GetRecipeVersionsAsync(string recipeCode, string? version)
        {
            var sql = @"
                SELECT 
                    rd.RecipeDetailsId, 
                    rd.RecipeCode, 
                    rd.RecipeName, 
                    rd.Version, 
                    rd.RecipeStatus, 
                    rd.ProductCode, 
                    pm.ItemName AS ProductName, 
                    rd.Timestamp 
                FROM RecipeDetails rd 
                LEFT JOIN ProductMasters pm ON pm.ItemCode = rd.ProductCode 
                WHERE rd.RecipeCode = @RecipeCode";

            if (!string.IsNullOrEmpty(version)) sql += " AND rd.Version = @Version";
            
            using var conn = Connection;
            var data = (await conn.QueryAsync<RecipeDto>(sql, new { RecipeCode = recipeCode, Version = version })).ToList();
            
            return ApiResponse<object>.Success(data);
        }

        public async Task<ApiResponse<ProductionOrderDto>> GetByIdAsync(int id)
        {
            string cacheKey = $"production_details:detail:{id}";
            var cached = await _cache.GetAsync<ApiResponse<ProductionOrderDto>>(cacheKey);
            if (cached != null) return cached;

            var sql = @"
              SELECT
                po.ProductionOrderId, po.ProductionLine, po.ProductCode, po.ProductionOrderNumber, 
                po.RecipeCode, po.RecipeVersion, po.Shift, po.PlannedStart, po.PlannedEnd, 
                CAST(CAST(po.Quantity AS FLOAT) AS DECIMAL(18,4)) AS Quantity, po.UnitOfMeasurement, po.LotNumber, po.timestamp, po.Plant, 
                po.Shopfloor, po.ProcessArea,
                CASE WHEN MAX(mc.ProductionOrderNumber) IS NOT NULL THEN -1 ELSE 0 END AS Status,
                pm.ItemName,
                rd.RecipeName,
                rd.RecipeDetailsId,
                CAST(CAST(p.PlanQuantity AS FLOAT) AS DECIMAL(18,4)) AS ProductQuantity,
                MAX(mc.MaxBatch) AS CurrentBatch,
                COUNT(DISTINCT b.BatchNumber) AS TotalBatches
              FROM ProductionOrders po
              LEFT JOIN ProductMasters pm ON po.ProductCode = pm.ItemCode
              LEFT JOIN (
                SELECT ProductCode, Version, MAX(RecipeDetailsId) as LatestId
                FROM RecipeDetails
                GROUP BY ProductCode, Version
              ) rd_latest ON rd_latest.ProductCode = po.ProductCode
                AND CAST(rd_latest.Version AS NVARCHAR) = CAST(po.RecipeVersion AS NVARCHAR)
              LEFT JOIN RecipeDetails rd ON rd.RecipeDetailsId = rd_latest.LatestId
              LEFT JOIN Products p ON po.ProductCode = p.ProductCode
              LEFT JOIN (
                SELECT LTRIM(RTRIM(ProductionOrderNumber)) AS ProductionOrderNumber, MAX(BatchCode) AS MaxBatch 
                FROM MESMaterialConsumption
                GROUP BY LTRIM(RTRIM(ProductionOrderNumber))
              ) mc ON LTRIM(RTRIM(po.ProductionOrderNumber)) = mc.ProductionOrderNumber
              LEFT JOIN Batches b ON b.ProductionOrderId = po.ProductionOrderId
              WHERE po.ProductionOrderId = @ProductionOrderId
              GROUP BY 
                po.ProductionOrderId, po.ProductionLine, po.ProductCode, po.ProductionOrderNumber, 
                po.RecipeCode, po.RecipeVersion, po.Shift, po.PlannedStart, po.PlannedEnd, 
                po.Quantity, po.UnitOfMeasurement, po.LotNumber, po.timestamp, po.Plant, 
                po.Shopfloor, po.ProcessArea, pm.ItemName, rd.RecipeName, rd.RecipeDetailsId, p.PlanQuantity";

            using var conn = Connection;
            var o = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { ProductionOrderId = id });
            if (o == null) return ApiResponse<ProductionOrderDto>.Error("Order not found", "404");

            var result = ApiResponse<ProductionOrderDto>.Success(new ProductionOrderDto
            {
                ProductionOrderId = o.ProductionOrderId,
                ProductionLine = o.ProductionLine,
                ProductionOrderNumber = o.ProductionOrderNumber,
                ProductCode = o.ProductCode,
                ProductName = o.ItemName,
                RecipeCode = o.RecipeCode,
                RecipeName = o.RecipeName,
                RecipeVersion = o.RecipeVersion?.ToString() ?? "",
                Shift = o.Shift,
                PlannedStart = o.PlannedStart,
                PlannedEnd = o.PlannedEnd,
                Quantity = o.Quantity,
                UnitOfMeasurement = o.UnitOfMeasurement,
                LotNumber = o.LotNumber,
                ProcessArea = o.ProcessArea,
                Status = o.Status != null ? Convert.ToInt32(o.Status) : null,
                CurrentBatch = o.CurrentBatch?.ToString() ?? "0",
                TotalBatches = o.TotalBatches != null ? Convert.ToInt32(o.TotalBatches) : 0,
                RecipeDetailsId = o.RecipeDetailsId != null ? Convert.ToInt32(o.RecipeDetailsId) : null,
                ProductQuantity = o.ProductQuantity != null ? Convert.ToDecimal(o.ProductQuantity) : null
            });

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
    }
}
