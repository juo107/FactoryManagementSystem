using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace FactoryManagementSystem.Services
{
    public class ProductionOrderDetailsService : IProductionOrderDetailsService
    {
        private readonly IConfiguration _config;

        public ProductionOrderDetailsService(IConfiguration config)
        {
            _config = config;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<object> GetBatchesAsync(int productionOrderId)
        {
            var sql = @"SELECT * FROM Batches WHERE ProductionOrderId = @ProductionOrderId";
            using var conn = Connection;
            var data = await conn.QueryAsync(sql, new { ProductionOrderId = productionOrderId });
            return new { success = true, data };
        }

        public async Task<object> GetIngredientsByProductAsync(string productionOrderNumber)
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
            
            return new {
                success = true,
                message = "Lấy danh sách ingredients thành công",
                productCode = rows.Any() ? rows[0].ProductCode : null,
                recipeVersion = rows.Any() ? rows[0].RecipeVersion : null,
                total = rows.Count,
                data = rows
            };
        }

        public async Task<object> GetMaterialConsumptionsAsync(string productionOrderNumber, int page, int limit)
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
                id = row.id,
                batchCode = row.batchCode,
                ingredientCode = row.ItemName != null
                  ? $"{(string)row.IngredientCode} - {(string)row.ItemName}"
                  : (string)row.IngredientCode,
                lot = row.lot ?? "",
                quantity = row.quantity,
                unitOfMeasurement = row.unitOfMeasurement ?? "",
                datetime = row.datetime,
                operator_ID = row.operator_ID,
                supplyMachine = row.supplyMachine,
                count = row.count ?? 0,
                request = row.request,
                respone = row.respone,
                status1 = row.status1,
                timestamp = row.timestamp
            });

            return new {
                success = true,
                message = "Lấy danh sách tiêu hao vật liệu thành công",
                page = pageNum,
                limit = pageLimit,
                data
            };
        }

        public async Task<object> GetMaterialConsumptionsExcludeBatchesAsync(string productionOrderNumber, int page, int limit, List<dynamic>? batchCodesWithMaterials)
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
                    if (b is JsonElement je)
                        return je.GetProperty("batchCode").GetString();
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
                return new {
                    success = true,
                    message = "Không có dữ liệu",
                    page = pageNum,
                    limit = pageLimit,
                    totalCount = 0,
                    totalPages = 0,
                    data = new List<object>()
                };
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
                id = row.id,
                productionOrderNumber = row.productionOrderNumber,
                batchCode = row.batchCode, // consistently NULL per legacy
                ingredientCode = row.ItemName != null
                  ? $"{(string)row.ingredientCode} - {(string)row.ItemName}"
                  : (string)row.ingredientCode,
                lot = row.lot,
                quantity = row.quantity,
                unitOfMeasurement = row.unitOfMeasurement,
                datetime = row.datetime,
                operator_ID = row.operator_ID,
                supplyMachine = row.supplyMachine,
                count = row.count ?? 0,
                request = row.request,
                respone = row.respone,
                status1 = row.status1,
                timestamp = row.timestamp
            });

            return new {
                success = true,
                message = "Lấy danh sách tiêu hao vật liệu (không thuộc batch) thành công",
                page = pageNum,
                limit = pageLimit,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageLimit),
                data
            };
        }

        public async Task<object> GetBatchCodesWithMaterialsAsync(string productionOrderNumber)
        {
            var sql = @"SELECT DISTINCT batchCode FROM MESMaterialConsumption WHERE ProductionOrderNumber = @productionOrderNumber ORDER BY batchCode ASC";
            using var conn = Connection;
            var rows = await conn.QueryAsync(sql, new { productionOrderNumber });
            return new {
                success = true,
                message = "Lấy danh sách batch codes có dữ liệu thành công",
                data = rows.Select(r => new { batchCode = r.batchCode })
            };
        }

        public async Task<object> GetRecipeVersionsAsync(string recipeCode, string? version)
        {
            var sql = @"SELECT rd.*, pm.ItemName FROM RecipeDetails rd LEFT JOIN ProductMasters pm ON pm.ItemCode = rd.ProductCode WHERE rd.RecipeCode = @RecipeCode";
            if (!string.IsNullOrEmpty(version)) sql += " AND rd.Version = @Version";
            using var conn = Connection;
            var data = await conn.QueryAsync(sql, new { RecipeCode = recipeCode, Version = version });
            return new { success = true, data };
        }

        public async Task<object> GetByIdAsync(int id)
        {
            var sql = @"
              SELECT
                po.ProductionOrderId,
                po.ProductionLine,
                po.ProductCode,
                po.ProductionOrderNumber,
                po.RecipeCode,
                po.RecipeVersion,
                po.Shift,
                po.PlannedStart,
                po.PlannedEnd,
                po.Quantity,
                po.UnitOfMeasurement,
                po.LotNumber,
                po.timestamp,
                po.Plant,
                po.Shopfloor,
                po.ProcessArea,
                po.Status,

                pm.ItemName,
                ing.PlanQuantity AS ProductQuantity,

                rd.RecipeName,
                MAX(rd.RecipeDetailsId) AS RecipeDetailsId,

                MAX(mc.BatchCode) AS CurrentBatch,
                COUNT(DISTINCT b.BatchNumber) AS TotalBatches

              FROM ProductionOrders po

              LEFT JOIN ProductMasters pm 
                ON po.ProductCode = pm.ItemCode

              LEFT JOIN Products ing 
                ON po.ProductCode = ing.ProductCode

              LEFT JOIN RecipeDetails rd 
                ON po.RecipeCode = rd.RecipeCode
               AND po.RecipeVersion = rd.Version

              LEFT JOIN MESMaterialConsumption mc
                ON mc.ProductionOrderNumber = po.ProductionOrderNumber

              LEFT JOIN Batches b
                ON b.ProductionOrderId = po.ProductionOrderId

              WHERE po.ProductionOrderId = @ProductionOrderId

              GROUP BY
                po.ProductionOrderId,
                po.ProductionLine,
                po.ProductCode,
                po.ProductionOrderNumber,
                po.RecipeCode,
                po.RecipeVersion,
                po.Shift,
                po.PlannedStart,
                po.PlannedEnd,
                po.Quantity,
                po.UnitOfMeasurement,
                po.LotNumber,
                po.timestamp,
                po.Plant,
                po.Shopfloor,
                po.ProcessArea,
                po.Status,
                pm.ItemName,
                ing.PlanQuantity,
                rd.RecipeName";

            using var conn = Connection;
            var order = await conn.QueryFirstOrDefaultAsync(sql, new { ProductionOrderId = id });
            if (order == null) return null;

            var data = new {
                ProductionOrderId = order.ProductionOrderId,
                ProductionLine = order.ProductionLine,
                ProductionOrderNumber = order.ProductionOrderNumber,
                ProductCode = order.ItemName != null ? $"{order.ProductCode} - {order.ItemName}" : order.ProductCode,
                RecipeCode = (order.RecipeName != null && order.RecipeCode != null) ? $"{order.RecipeCode} - {order.RecipeName}" : order.RecipeCode,
                RecipeVersion = order.RecipeVersion,
                Shift = order.Shift,
                PlannedStart = order.PlannedStart,
                PlannedEnd = order.PlannedEnd,
                Quantity = order.Quantity,
                UnitOfMeasurement = order.UnitOfMeasurement,
                LotNumber = order.LotNumber,
                timestamp = order.timestamp,
                Plant = order.Plant,
                Shopfloor = order.Shopfloor,
                ProcessArea = order.ProcessArea,
                Status = (int)order.Status,
                ItemName = order.ItemName,
                RecipeName = order.RecipeName,
                RecipeDetailsId = order.RecipeDetailsId,
                CurrentBatch = order.CurrentBatch,
                TotalBatches = order.TotalBatches,
                ProductQuantity = order.ProductQuantity
            };

            return new {
                success = true,
                message = "Lấy chi tiết đơn hàng thành công",
                data
            };
        }
    }
}
