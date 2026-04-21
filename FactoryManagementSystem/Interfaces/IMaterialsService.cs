using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Materials;

namespace FactoryManagementSystem.Interfaces
{
    public interface IMaterialsService
    {
        Task<ApiResponse<IEnumerable<MaterialProductionOrderDto>>> GetProductionOrdersAsync(IQueryCollection query);
        Task<ApiResponse<IEnumerable<MaterialBatchDto>>> GetBatchCodesAsync(string? productionOrderNumber, IQueryCollection query);
        Task<ApiResponse<IEnumerable<MaterialIngredientDto>>> GetIngredientsAsync(string? productionOrderNumber, string? batchCode, IQueryCollection query);
        Task<ApiResponse<IEnumerable<MaterialSimpleDto>>> GetShiftsAsync(IQueryCollection query);
        Task<ApiResponse<PagedResponse<MaterialConsumptionDto>>> SearchAsync(IQueryCollection query, int page, int pageSize);
        Task<ApiResponse<object>> GetStatsSearchAsync(IQueryCollection query);
    }
}
