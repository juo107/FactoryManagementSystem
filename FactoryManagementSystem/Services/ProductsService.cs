using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using FactoryManagementSystem.Interfaces;
using System.Text.Json;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Products;

namespace FactoryManagementSystem.Services
{
    public class ProductsService : IProductsService
    {
        private readonly IConfiguration _config;
        private readonly IRedisCacheService _cache;

        public ProductsService(IConfiguration config, IRedisCacheService cache)
        {
            _config = config;
            _cache = cache;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<ApiResponse<IEnumerable<string>>> GetTypesAsync()
        {
            string cacheKey = "products:types";
            var cached = await _cache.GetAsync<ApiResponse<IEnumerable<string>>>(cacheKey);
            if (cached != null) return cached;

            using var conn = Connection;
            var sql = "SELECT DISTINCT Item_Type FROM ProductMasters WHERE Item_Type IS NOT NULL ORDER BY Item_Type";
            var types = await conn.QueryAsync<string>(sql);
            var result = ApiResponse<IEnumerable<string>>.Success(types);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }

        public async Task<ApiResponse<PagedResponse<ProductDto>>> SearchAsync(string? q, string? status, string? statuses, string? type, string? types, int page, int pageSize)
        {
            var pageInt = Math.Max(page, 1);
            var pageSizeInt = Math.Min(Math.Max(pageSize, 1), 100);
            var offset = (pageInt - 1) * pageSizeInt;

            var (whereSql, p) = BuildWhereClause(q, status, statuses, type, types);
            using var conn = Connection;

            var total = await conn.ExecuteScalarAsync<int>($@"SELECT COUNT(*) FROM ProductMasters p {whereSql}", p);

            var dataSql = $@"
                SELECT 
                    p.ProductMasterId, p.ItemCode, p.ItemName, p.Item_Type, p.[Group], p.Category, p.Brand, 
                    p.BaseUnit, p.InventoryUnit, p.Item_Status, p.[timestamp] as Timestamp,
                    JSON_QUERY((
                        SELECT m.MHUTypeId, m.FromUnit, m.ToUnit, m.Conversion 
                        FROM MHUTypes m WHERE m.ProductMasterId = p.ProductMasterId 
                        FOR JSON PATH
                    )) AS MhuTypesJson
                FROM ProductMasters p 
                {whereSql} 
                ORDER BY p.[timestamp] DESC 
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            p.Add("offset", offset);
            p.Add("pageSize", pageSizeInt);

            var results = await conn.QueryAsync<ProductDto>(dataSql, p);
            
            foreach (var item in results)
            {
                if (!string.IsNullOrEmpty(item.MhuTypesJson))
                {
                    item.MhuTypes = JsonSerializer.Deserialize<List<MhuTypeDto>>(item.MhuTypesJson) ?? new();
                }
            }

            return ApiResponse<PagedResponse<ProductDto>>.Success(
                new PagedResponse<ProductDto>(results, total, pageInt, pageSizeInt)
            );
        }

        public async Task<ApiResponse<object>> GetStatsSearchAsync(string? q, string? status, string? statuses, string? type, string? types)
        {
            var (whereSql, p) = BuildWhereClause(q, status, statuses, type, types);
            using var conn = Connection;
            var countSql = $@"SELECT COUNT(*) FROM ProductMasters p {whereSql}";
            var total = await conn.ExecuteScalarAsync<int>(countSql, p);
            return ApiResponse<object>.Success(new { Total = total });
        }

        public async Task<ApiResponse<ProductDto>> GetByIdAsync(string id)
        {
            var sql = @"
                SELECT 
                    p.ProductMasterId, p.ItemCode, p.ItemName, p.Item_Type, p.[Group], p.Category, p.Brand, 
                    p.BaseUnit, p.InventoryUnit, p.Item_Status, p.[timestamp] as Timestamp,
                    JSON_QUERY((
                        SELECT m.MHUTypeId, m.FromUnit, m.ToUnit, m.Conversion 
                        FROM MHUTypes m WHERE m.ProductMasterId = p.ProductMasterId 
                        FOR JSON PATH
                    )) AS MhuTypesJson
                FROM ProductMasters p 
                WHERE p.ItemCode = @id";

            using var conn = Connection;
            var product = await conn.QueryFirstOrDefaultAsync<ProductDto>(sql, new { id });
            
            if (product == null) return ApiResponse<ProductDto>.Error("Product not found", "404");

            if (!string.IsNullOrEmpty(product.MhuTypesJson))
            {
                product.MhuTypes = JsonSerializer.Deserialize<List<MhuTypeDto>>(product.MhuTypesJson) ?? new();
            }

            return ApiResponse<ProductDto>.Success(product);
        }

        private (string whereSql, DynamicParameters p) BuildWhereClause(string? q, string? status, string? statuses, string? type, string? types)
        {
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
                whereClauses.Add("p.Item_Type IN @typesList");
                p.Add("typesList", typesList);
            }
            else if (!string.IsNullOrWhiteSpace(type))
            {
                whereClauses.Add("p.Item_Type = @type");
                p.Add("type", type);
            }

            var whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
            return (whereSql, p);
        }
    }
}
