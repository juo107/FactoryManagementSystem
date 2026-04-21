using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.ProductionOrders;

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
                i.IngredientCode,
                i.Quantity,
                i.UnitOfMeasurement,
                pm.ItemName,
                po.ProductCode,
                po.RecipeVersion
              FROM ProductionOrders po
              JOIN RecipeDetails rd 
                ON rd.ProductCode = po.ProductCode 
              AND rd.Version = po.RecipeVersion
              JOIN Processes p 
                ON p.RecipeDetailsId = rd.RecipeDetailsId
              JOIN Ingredients i 
                ON i.ProcessId = p.ProcessId
              LEFT JOIN ProductMasters pm 
                ON pm.ItemCode = i.IngredientCode
              WHERE po.ProductionOrderNumber = @prodOrderNum
            ";

            using var conn = Connection;
            var rows = (await conn.QueryAsync(sql, new { prodOrderNum = productionOrderNumber.Trim() })).ToList();
            
            return ApiResponse<object>.Success(new {
                productCode = rows.Any() ? rows[0].ProductCode : null,
                recipeVersion = rows.Any() ? rows[0].RecipeVersion : null,
                total = rows.Count,
                data = rows
            });
        }

        public async Task<ApiResponse<object>> GetMaterialConsumptionsAsync(string productionOrderNumber, int page, int limit)
        {
            var pageNum = Math.Max(1, page);
            var pageLimit = Math.Min(100, Math.Max(1, limit));
            var from = (pageNum - 1) * pageLimit + 1;
            var to = pageNum * pageLimit;

            var sql = @"
                ;WITH BatchCTE AS (
                  SELECT
                    b.BatchNumber AS batchCode,
                    ROW_NUMBER() OVER (ORDER BY b.BatchNumber) AS rn
                  FROM Batches b
                  JOIN ProductionOrders po
                    ON po.ProductionOrderId = b.ProductionOrderId
                  WHERE po.ProductionOrderNumber = @prodOrderNum
                ),
                PagedBatch AS (
                  SELECT batchCode
                  FROM BatchCTE
                  WHERE rn BETWEEN @from AND @to
                ),
                RecipeIngredient AS (
                  SELECT DISTINCT
                    i.IngredientCode,
                    pm.ItemName
                  FROM ProductionOrders po
                  JOIN RecipeDetails rd
                    ON rd.ProductCode = po.ProductCode
                  AND rd.Version = po.RecipeVersion
                  JOIN Processes p ON p.RecipeDetailsId = rd.RecipeDetailsId
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
                  pb.batchCode,
                  r.IngredientCode,
                  r.ItemName,
                  mc.id,
                  mc.lot,
                  mc.quantity,
                  COALESCE(mc.unitOfMeasurement, ing.UnitOfMeasurement) AS unitOfMeasurement,
                  mc.datetime,
                  mc.operator_ID,
                  mc.supplyMachine,
                  mc.count,
                  mc.request,
                  mc.respone,
                  mc.status1,
                  mc.timestamp
                FROM PagedBatch pb
                CROSS JOIN RecipeIngredient r
                LEFT JOIN MESMaterialConsumption mc
                  ON mc.productionOrderNumber = @prodOrderNum
                AND mc.batchCode = pb.batchCode
                AND mc.ingredientCode = r.IngredientCode
                LEFT JOIN Ingredients ing
                  ON ing.IngredientCode = r.IngredientCode

                UNION ALL

                SELECT
                  mc.batchCode,
                  e.IngredientCode,
                  e.ItemName,
                  mc.id,
                  mc.lot,
                  mc.quantity,
                  mc.unitOfMeasurement,
                  mc.datetime,
                  mc.operator_ID,
                  mc.supplyMachine,
                  mc.count,
                  mc.request,
                  mc.respone,
                  mc.status1,
                  mc.timestamp
                FROM MESMaterialConsumption mc
                JOIN PagedBatch pb
                  ON pb.batchCode = mc.batchCode
                JOIN ExtraIngredient e
                  ON e.IngredientCode = mc.ingredientCode
                WHERE mc.productionOrderNumber = @prodOrderNum

                ORDER BY batchCode, IngredientCode;
            ";

            using var conn = Connection;
            var rows = await conn.QueryAsync(sql, new { prodOrderNum = productionOrderNumber.Trim(), from, to });
            var data = rows.Select(row => new {
                Id = row.id,
                BatchCode = row.batchCode,
                IngredientCode = row.ItemName != null
                  ? $"{(string)row.IngredientCode} - {(string)row.ItemName}"
                  : (string)row.IngredientCode,
                Lot = row.lot ?? "",
                Quantity = row.quantity,
                UnitOfMeasurement = row.unitOfMeasurement ?? "",
                Datetime = row.datetime,
                Operator_ID = row.operator_ID,
                SupplyMachine = row.supplyMachine,
                Count = row.count ?? 0,
                Request = row.request,
                Respone = row.respone,
                Status1 = row.status1,
                Timestamp = row.timestamp
            });

            return ApiResponse<object>.Success(new {
                page = pageNum,
                limit = pageLimit,
                items = data
            });
        }

        public async Task<ApiResponse<object>> GetMaterialConsumptionsExcludeBatchesAsync(string productionOrderNumber, int page, int limit, List<dynamic>? batchCodesWithMaterials)
        {
            var pageNum = Math.Max(1, page);
            var pageLimit = Math.Min(100, Math.Max(1, limit));
            var offset = (pageNum - 1) * pageLimit;

            var p = new DynamicParameters();
            p.Add("prodOrderNum", productionOrderNumber.Trim());

            string batchFilterSql = "";
            if (batchCodesWithMaterials != null && batchCodesWithMaterials.Any())
            {
                var batchNumbers = batchCodesWithMaterials.Select(b => {
                    // Handle dynamic types safely
                    return (string)b.batchCode;
                }).Where(x => x != null).ToList();
                var ps = new List<string>();
                for (int i = 0; i < batchNumbers.Count; i++)
                {
                    var key = $"batch{i}";
                    ps.Add("@" + key);
                    p.Add(key, batchNumbers[i]);
                }
                batchFilterSql = $" OR mc.batchCode IN ({string.Join(",", ps)})";
            }

            var countSql = $@"
                SELECT COUNT(*) AS totalCount
                FROM MESMaterialConsumption mc WITH (NOLOCK)
                WHERE mc.ProductionOrderNumber = @prodOrderNum
                  AND (
                    mc.batchCode IS NULL
                    {batchFilterSql}
                  )
            ";

            using var conn = Connection;
            var totalCount = await conn.ExecuteScalarAsync<int>(countSql, p);
            if (totalCount == 0)
            {
                return ApiResponse<object>.Success(new {
                    page = pageNum,
                    limit = pageLimit,
                    totalCount = 0,
                    totalPages = 0,
                    items = new List<object>()
                });
            }

            p.Add("offset", offset);
            p.Add("limit", pageLimit);

            var dataSql = $@"
                SELECT
                    mc.id,
                    mc.productionOrderNumber,
                    mc.batchCode,
                    mc.ingredientCode,
                    pm.ItemName,
                    mc.lot,
                    mc.quantity,
                    mc.unitOfMeasurement,
                    mc.datetime,
                    mc.operator_ID,
                    mc.supplyMachine,
                    mc.count,
                    mc.request,
                    mc.respone,
                    mc.status1,
                    mc.timestamp
                FROM MESMaterialConsumption mc WITH (NOLOCK)
                LEFT JOIN ProductMasters pm WITH (NOLOCK)
                  ON pm.ItemCode = mc.ingredientCode
                WHERE mc.ProductionOrderNumber = @prodOrderNum
                  AND (
                    mc.batchCode IS NULL
                    {batchFilterSql}
                  )
                ORDER BY mc.id DESC
                OFFSET @offset ROWS
                FETCH NEXT @limit ROWS ONLY
            ";

            var rows = await conn.QueryAsync(dataSql, p);

            var data = rows.Select(row => new {
                Id = row.id,
                ProductionOrderNumber = row.productionOrderNumber,
                BatchCode = row.batchCode,
                IngredientCode = row.ItemName != null
                  ? $"{(string)row.ingredientCode} - {(string)row.ItemName}"
                  : (string)row.ingredientCode,
                Lot = row.lot,
                Quantity = row.quantity,
                UnitOfMeasurement = row.unitOfMeasurement,
                Datetime = row.datetime,
                Operator_ID = row.operator_ID,
                SupplyMachine = row.supplyMachine,
                Count = row.count ?? 0,
                Request = row.request,
                Respone = row.respone,
                Status1 = row.status1,
                Timestamp = row.timestamp
            });

            return ApiResponse<object>.Success(new {
                page = pageNum,
                limit = pageLimit,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageLimit),
                items = data
            });
        }

        public async Task<ApiResponse<object>> GetBatchCodesWithMaterialsAsync(string productionOrderNumber)
        {
            var sql = @"SELECT DISTINCT batchCode FROM MESMaterialConsumption WHERE ProductionOrderNumber = @productionOrderNumber ORDER BY batchCode ASC";
            using var conn = Connection;
            var rows = await conn.QueryAsync(sql, new { productionOrderNumber });
            return ApiResponse<object>.Success(rows.Select(r => new { BatchCode = r.batchCode }));
        }

        public async Task<ApiResponse<object>> GetRecipeVersionsAsync(string recipeCode, string? version)
        {
            var sql = @"SELECT rd.*, pm.ItemName FROM RecipeDetails rd LEFT JOIN ProductMasters pm ON pm.ItemCode = rd.ProductCode WHERE rd.RecipeCode = @RecipeCode";
            if (!string.IsNullOrEmpty(version)) sql += " AND rd.Version = @Version";
            using var conn = Connection;
            var data = await conn.QueryAsync(sql, new { RecipeCode = recipeCode, Version = version });
            return ApiResponse<object>.Success(data);
        }

        public async Task<ApiResponse<ProductionOrderDto>> GetByIdAsync(int id)
        {
            string cacheKey = $"production_details:detail:{id}";
            var cached = await _cache.GetAsync<ApiResponse<ProductionOrderDto>>(cacheKey);
            if (cached != null) return cached;

            var sql = @"
              SELECT
                po.*,
                pm.ItemName,
                rd.RecipeName,
                rd.RecipeDetailsId,
                MAX(mc.BatchCode) AS CurrentBatch,
                COUNT(DISTINCT b.BatchNumber) AS TotalBatches
              FROM ProductionOrders po
              LEFT JOIN ProductMasters pm ON po.ProductCode = pm.ItemCode
              LEFT JOIN RecipeDetails rd ON po.RecipeCode = rd.RecipeCode AND po.RecipeVersion = rd.Version
              LEFT JOIN MESMaterialConsumption mc ON mc.ProductionOrderNumber = po.ProductionOrderNumber
              LEFT JOIN Batches b ON b.ProductionOrderId = po.ProductionOrderId
              WHERE po.ProductionOrderId = @ProductionOrderId
              GROUP BY po.ProductionOrderId, po.ProductionLine, po.ProductCode, po.ProductionOrderNumber, 
                       po.RecipeCode, po.RecipeVersion, po.Shift, po.PlannedStart, po.PlannedEnd, 
                       po.Quantity, po.UnitOfMeasurement, po.LotNumber, po.timestamp, po.Plant, 
                       po.Shopfloor, po.ProcessArea, po.Status, pm.ItemName, rd.RecipeName, rd.RecipeDetailsId";

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
                TotalBatches = o.TotalBatches != null ? Convert.ToInt32(o.TotalBatches) : 0
            });

            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(10));
            return result;
        }
    }
}
