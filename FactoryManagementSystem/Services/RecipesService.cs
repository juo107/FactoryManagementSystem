using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Recipes;

namespace FactoryManagementSystem.Services
{
    public class RecipesService : IRecipesService
    {
        private readonly IConfiguration _config;
        private readonly IRedisCacheService _cache;

        public RecipesService(IConfiguration config, IRedisCacheService cache)
        {
            _config = config;
            _cache = cache;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<ApiResponse<object>> GetStatsSearchAsync(string? search, string? status, string? statuses)
        {
            var whereCommon = new StringBuilder("1=1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(search))
            {
                parameters.Add("search", $"%{search.Trim()}%");
                whereCommon.Append(@" AND (RecipeCode LIKE @search OR ProductCode LIKE @search OR ProductName LIKE @search)");
            }

            string statusClause = "";
            if (!string.IsNullOrWhiteSpace(statuses))
            {
                var list = statuses.Split(',').Select(s => s.Trim().ToLower()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var parts = new List<string>();
                if (list.Contains("active")) parts.Add("RecipeStatus = 'Active'");
                if (list.Contains("inactive")) parts.Add("(RecipeStatus NOT IN ('Active') OR RecipeStatus IS NULL)");
                if (parts.Any()) statusClause = $" AND ({string.Join(" OR ", parts)})";
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "active") statusClause = " AND RecipeStatus = 'Active'";
                else if (status == "inactive") statusClause = " AND (RecipeStatus NOT IN ('Active') OR RecipeStatus IS NULL)";
            }

            var whereTotal = whereCommon.ToString() + statusClause;
            var sql = $@"SELECT (SELECT COUNT(*) FROM RecipeDetails WHERE {whereTotal}) as total, (SELECT COUNT(*) FROM RecipeDetails WHERE {whereCommon} AND RecipeStatus = 'Active'{statusClause}) as active, (SELECT COUNT(DISTINCT Version) FROM RecipeDetails WHERE {whereTotal}) as totalVersions";

            using var conn = Connection;
            var stats = await conn.QueryFirstOrDefaultAsync(sql, parameters);
            var result = ApiResponse<object>.Success(new {
                total = stats?.total ?? 0,
                active = stats?.active ?? 0,
                totalVersions = stats?.totalVersions ?? 0,
                draft = 0
            });
            return result;
        }

        public async Task<ApiResponse<PagedResponse<RecipeDto>>> SearchAsync(int page, int limit, string? search, string? status, string? statuses)
        {
            page = Math.Max(1, page);
            limit = Math.Max(1, Math.Min(100, limit));
            var skip = (page - 1) * limit;

            var where = new StringBuilder("1=1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(search))
            {
                parameters.Add("search", $"%{search.Trim()}%");
                where.Append(@" AND (RecipeCode LIKE @search OR ProductCode LIKE @search OR ProductName LIKE @search)");
            }

            if (!string.IsNullOrWhiteSpace(statuses))
            {
                var list = statuses.Split(',').Select(s => s.Trim().ToLower()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var parts = new List<string>();
                if (list.Contains("active")) parts.Add("RecipeStatus = 'Active'");
                if (list.Contains("inactive")) parts.Add("(RecipeStatus NOT IN ('Active') OR RecipeStatus IS NULL)");
                if (parts.Any()) where.Append($" AND ({string.Join(" OR ", parts)})");
            }
            else if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "active") where.Append(" AND RecipeStatus = 'Active'");
                else if (status == "inactive") where.Append(" AND (RecipeStatus NOT IN ('Active') OR RecipeStatus IS NULL)");
            }

            using var conn = Connection;
            var countSql = $"SELECT COUNT(*) FROM RecipeDetails WHERE {where}";
            var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
            var totalPages = total > 0 ? (int)Math.Ceiling(total / (double)limit) : 1;

            var dataSql = $@"SELECT * FROM RecipeDetails WHERE {where} ORDER BY RecipeDetailsId DESC OFFSET @skip ROWS FETCH NEXT @limit ROWS ONLY";
            parameters.Add("skip", skip);
            parameters.Add("limit", limit);

            var data = await conn.QueryAsync<RecipeDto>(dataSql, parameters);
            var result = ApiResponse<PagedResponse<RecipeDto>>.Success(
                new PagedResponse<RecipeDto>(data, total, page, limit)
            );
            return result;
        }

        public async Task<ApiResponse<RecipeDetailResponseDto>> GetByIdAsync(int id)
        {
            string cacheKey = $"recipes:detail:{id}";
            var cached = await _cache.GetAsync<ApiResponse<RecipeDetailResponseDto>>(cacheKey);
            if (cached != null) return cached;

            using var conn = Connection;
            var recipe = await conn.QueryFirstOrDefaultAsync<RecipeDto>("SELECT * FROM RecipeDetails WHERE RecipeDetailsId = @id", new { id });
            if (recipe == null) return ApiResponse<RecipeDetailResponseDto>.Error("Recipe not found", "404");

            var processes = (await conn.QueryAsync("SELECT * FROM Processes WHERE RecipeDetailsId = @id", new { id })).ToList();
            var processIds = processes.Select(p => (int)p.ProcessId).ToList();

            IEnumerable<dynamic> ingredients = new List<dynamic>();
            if (processIds.Any()) { ingredients = await conn.QueryAsync(@"SELECT i.*, pm.ItemName FROM Ingredients i LEFT JOIN ProductMasters pm ON i.IngredientCode = pm.ItemCode WHERE i.ProcessId IN @processIds", new { processIds }); }

            IEnumerable<dynamic> products = new List<dynamic>();
            if (recipe.ProductCode != null) { products = await conn.QueryAsync(@"SELECT p.*, pm.ItemName FROM Products p LEFT JOIN ProductMasters pm ON p.ProductCode = pm.ItemCode WHERE p.ProductCode = @productCode", new { productCode = (string)recipe.ProductCode }); }

            IEnumerable<dynamic> byProducts = new List<dynamic>();
            if (recipe.ProductCode != null) { byProducts = await conn.QueryAsync(@"SELECT * FROM ByProducts WHERE ByProductCode = @productCode", new { productCode = (string)recipe.ProductCode }); }

            IEnumerable<dynamic> parameters = new List<dynamic>();
            if (processIds.Any()) { parameters = await conn.QueryAsync(@"SELECT p.*, ppm.Name as ParameterName FROM Parameters p LEFT JOIN ProcessParameterMasters ppm ON p.Code = ppm.Code WHERE p.ProcessId IN @processIds", new { processIds }); }

            var responseData = new RecipeDetailResponseDto
            {
                Recipe = recipe,
                Processes = processes,
                Ingredients = ingredients,
                Products = products,
                ByProducts = byProducts,
                Parameters = parameters
            };

            var result = ApiResponse<RecipeDetailResponseDto>.Success(responseData);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }
    }
}
