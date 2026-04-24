using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;

namespace FactoryManagementSystem.Services
{
    public class SuggestionsService : ISuggestionsService
    {
        private readonly IConfiguration _config;
        private readonly IRedisCacheService _cache;

        // Whitelist để bảo mật: Chỉ cho phép các bảng và cột này được query động
        private readonly Dictionary<string, string[]> _allowedMap = new()
        {
            { "ProductMasters", new[] { "ItemCode", "ItemName", "Group", "Category" } },
            { "ProductionOrders", new[] { "ProductionOrderNumber", "ProductCode", "ProductName", "RecipeCode", "ProcessArea" } },
            { "RecipeDetails", new[] { "RecipeCode", "RecipeName", "ProductCode" } },
            { "MESMaterialConsumption", new[] { "BatchCode", "IngredientCode", "ProductionOrderNumber" } },
            { "MESCompleteBatch", new[] { "ProductionOrder", "BatchNumber", "MachineCode", "ProductCode" } }
        };

        public SuggestionsService(IConfiguration config, IRedisCacheService cache)
        {
            _config = config;
            _cache = cache;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<ApiResponse<IEnumerable<SuggestionDto>>> GetSuggestionsAsync(string table, string column, string q)
        {
            // 1. Kiểm tra bảo mật (Whitelist)
            if (!_allowedMap.ContainsKey(table))
            {
                return ApiResponse<IEnumerable<SuggestionDto>>.Error("Bảng không hợp lệ.");
            }

            var allowedColumns = _allowedMap[table];
            var requestedColumns = column.Split(',').Select(c => c.Trim()).ToList();

            if (requestedColumns.Any(c => !allowedColumns.Contains(c)))
            {
                return ApiResponse<IEnumerable<SuggestionDto>>.Error("Một hoặc nhiều cột không hợp lệ.");
            }

            string keyword = (q ?? "").Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                return ApiResponse<IEnumerable<SuggestionDto>>.Success(new List<SuggestionDto>());
            }

            // 2. Kiểm tra Cache
            string cacheKey = $"suggestions_v2:{table}:{column}:{keyword.ToLower()}";
            var cached = await _cache.GetAsync<ApiResponse<IEnumerable<SuggestionDto>>>(cacheKey);
            if (cached != null) return cached;

            // 3. Truy vấn Database (TOP 5)
            using var conn = Connection;
            
            // Xây dựng chuỗi chọn cột để tạo Label (ví dụ: ItemCode + ' - ' + ItemName)
            // Lưu ý: Cần CAST sang NVARCHAR để tránh lỗi nếu có cột không phải chuỗi
            string labelExpression = requestedColumns.Count > 1 
                ? string.Join(" + ' | ' + ", requestedColumns.Select(c => $"ISNULL(CAST([{c}] AS NVARCHAR(MAX)), '')"))
                : $"[{requestedColumns[0]}]";

            string valueColumn = requestedColumns[0]; // Cột đầu tiên luôn làm giá trị thực tế (Value)

            // Xây dựng điều kiện WHERE
            var whereConditions = requestedColumns.Select(c => $"[{c}] LIKE '%' + @q + '%'");
            var whereClause = string.Join(" OR ", whereConditions);

            var sql = $@"
                SELECT DISTINCT TOP 5 
                    ({labelExpression}) as Label,
                    [{valueColumn}] as Value
                FROM [{table}]
                WHERE {whereClause}
                ORDER BY Value";

            var suggestions = await conn.QueryAsync<SuggestionDto>(sql, new { q = keyword });
            var result = ApiResponse<IEnumerable<SuggestionDto>>.Success(suggestions);

            // 4. Lưu Cache trong 5 phút
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
    }
}
