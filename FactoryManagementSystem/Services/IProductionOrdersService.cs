using System.Threading.Tasks;

namespace FactoryManagementSystem.Services
{
    public interface IProductionOrdersService
    {
        Task<object> GetFiltersAsync(string dateFrom, string dateTo);
        Task<object> GetFiltersV2Async(string dateFrom, string dateTo);
        Task<object> GetStatsSearchAsync(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses);
        Task<object> GetStatsSearchV2Async(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses, string pos, string batchIds);
        Task<object> SearchAsync(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, int page, int limit, int total);
        Task<object> SearchV2Async(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, string? pos, string? batchIds, int page, int limit, int total);
    }
}
