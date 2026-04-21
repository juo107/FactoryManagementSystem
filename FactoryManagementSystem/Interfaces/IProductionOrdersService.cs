using System.Collections.Generic;
using System.Threading.Tasks;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.ProductionOrders;

namespace FactoryManagementSystem.Interfaces
{
    public interface IProductionOrdersService
    {
        Task<ApiResponse<OrderFiltersDto>> GetFiltersAsync(string dateFrom, string dateTo);
        Task<ApiResponse<OrderFiltersDto>> GetFiltersV2Async(string dateFrom, string dateTo);
        Task<ApiResponse<OrderStatsDto>> GetStatsSearchAsync(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses);
        Task<ApiResponse<OrderStatsDto>> GetStatsSearchV2Async(string searchQuery, string dateFrom, string dateTo, string processAreas, string shifts, string statuses, string pos, string batchIds);
        Task<ApiResponse<PagedResponse<ProductionOrderDto>>> SearchAsync(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, int page, int limit, int total);
        Task<ApiResponse<PagedResponse<ProductionOrderDto>>> SearchV2Async(string? searchQuery, string? dateFrom, string? dateTo, string? processAreas, string? shifts, string? statuses, string? pos, string? batchIds, int page, int limit, int total);
    }
}
