using Dapper;
using FactoryManagementSystem.DTOs;
using FactoryManagementSystem.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FactoryManagementSystem.Services
{
    public class MESCompleteBatchService : IMESCompleteBatchService
    {
        private readonly IConfiguration _config;
        private readonly IRedisCacheService _cache;

        public MESCompleteBatchService(IConfiguration config, IRedisCacheService cache)
        {
            _config = config;
            _cache = cache;
        }

        private IDbConnection Connection => new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<MESCompleteBatchResponse> SearchAsync(MESCompleteBatchSearchParams paramsDto)
        {
            string cacheKey = $"mes_complete_batch:search:{paramsDto.Page}:{paramsDto.Limit}:{paramsDto.SearchQuery}:{paramsDto.ProductionOrder}:{paramsDto.BatchNumber}:{paramsDto.MachineCode}:{paramsDto.DateFrom}:{paramsDto.DateTo}:{paramsDto.TransferStatus}";
            
            var cached = await _cache.GetAsync<MESCompleteBatchResponse>(cacheKey);
            if (cached != null) return cached;

            using var conn = Connection;
            var whereClauses = new List<string>();
            
            string q = !string.IsNullOrWhiteSpace(paramsDto.SearchQuery) ? $"%{paramsDto.SearchQuery.Trim()}%" : string.Empty;
            string po = paramsDto.ProductionOrder?.Trim() ?? string.Empty;
            string bn = paramsDto.BatchNumber?.Trim() ?? string.Empty;
            string mc = paramsDto.MachineCode?.Trim() ?? string.Empty;
            
            DateTime? df = null;
            if (!string.IsNullOrEmpty(paramsDto.DateFrom) && DateTime.TryParse(paramsDto.DateFrom, out var dFrom)) df = dFrom;

            DateTime? dt = null;
            if (!string.IsNullOrEmpty(paramsDto.DateTo) && DateTime.TryParse(paramsDto.DateTo, out var dTo)) dt = dTo;

            if (!string.IsNullOrEmpty(q)) whereClauses.Add("(t.ProductionOrder LIKE @q OR t.BatchNumber LIKE @q OR t.ProductCode LIKE @q OR t.MachineCode LIKE @q)");
            if (!string.IsNullOrEmpty(po)) whereClauses.Add("t.ProductionOrder = @po");
            if (!string.IsNullOrEmpty(bn)) whereClauses.Add("t.BatchNumber = @bn");
            if (!string.IsNullOrEmpty(mc)) whereClauses.Add("t.MachineCode = @mc");
            if (!string.IsNullOrEmpty(paramsDto.TransferStatus)) whereClauses.Add("t.TransferStatus = @ts");
            if (df.HasValue) whereClauses.Add("t.CreatedAt >= @df");
            if (dt.HasValue) whereClauses.Add("t.CreatedAt < DATEADD(day, 1, @dt)");

            string whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            int offset = (paramsDto.Page - 1) * paramsDto.Limit;

            var sql = $@"
                SELECT COUNT(*) FROM MESCompleteBatch t {whereClause};

                SELECT 
                    t.Id, t.ProductionOrder, t.BatchNumber, t.BatchSize, t.BatchUOM, t.ProductCode, 
                    p.ItemName as ProductName, t.MachineCode, 
                    t.StartTime, t.EndTime, t.TransferStatus, t.RetryCount, t.NextRetryAt, t.ProcessingAt, 
                    t.RequestJson, t.ResponseContent, t.SentAt, t.CreatedAt, t.UpdatedAt
                FROM MESCompleteBatch t
                LEFT JOIN (
                    SELECT DISTINCT pr.ProductionOrderNumber, pm.ItemName
                    FROM ProductionOrders pr
                    LEFT JOIN ProductMasters pm ON pr.ProductCode = pm.ItemCode
                    WHERE pr.ProductionOrderNumber IS NOT NULL
                ) p ON t.ProductionOrder = p.ProductionOrderNumber
                {whereClause}
                ORDER BY t.Id DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
            ";

            using var multi = await conn.QueryMultipleAsync(sql, new { 
                q, po, bn, mc, 
                ts = paramsDto.TransferStatus, 
                df, dt, 
                offset, 
                limit = paramsDto.Limit 
            });

            int total = await multi.ReadFirstAsync<int>();
            IEnumerable<MESCompleteBatchDto> data = await multi.ReadAsync<MESCompleteBatchDto>();

            var response = new MESCompleteBatchResponse
            {
                Total = total,
                Page = paramsDto.Page,
                Limit = paramsDto.Limit,
                Data = data.ToList()
            };

            await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));
            return response;
        }

        public async Task<IEnumerable<string>> GetUniqueValuesAsync(string column)
        {
            var allowedColumns = new[] { "BatchNumber", "MachineCode", "ProductCode", "ProductionOrder" };
            if (!allowedColumns.Contains(column)) return Enumerable.Empty<string>();

            using var conn = Connection;
            var sql = $"SELECT DISTINCT {column} FROM MESCompleteBatch WHERE {column} IS NOT NULL AND {column} <> '' ORDER BY LEN({column}) ASC, {column} ASC";
            return await conn.QueryAsync<string>(sql);
        }
    }
}
