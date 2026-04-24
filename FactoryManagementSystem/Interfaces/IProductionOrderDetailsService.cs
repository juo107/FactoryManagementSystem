using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Materials;
using FactoryManagementSystem.DTOs.ProductionOrders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FactoryManagementSystem.Interfaces
{
    public interface IProductionOrderDetailsService
    {
        Task<ApiResponse<IEnumerable<BatchDto>>> GetBatchesAsync(int productionOrderId);
        Task<ApiResponse<object>> GetIngredientsByProductAsync(string productionOrderNumber);
        Task<ApiResponse<object>> GetMaterialConsumptionsAsync(string productionOrderNumber, int page, int limit, List<string>? batches = null);
        Task<ApiResponse<object>> GetMaterialConsumptionsExcludeBatchesAsync(string productionOrderNumber,int page,int limit, List<BatchFilterDto>? batchFilters); 
        Task<ApiResponse<object>> GetBatchCodesWithMaterialsAsync(string productionOrderNumber);
        Task<ApiResponse<object>> GetRecipeVersionsAsync(string recipeCode, string? version);
        Task<ApiResponse<ProductionOrderDto>> GetByIdAsync(int id);
    }
}
