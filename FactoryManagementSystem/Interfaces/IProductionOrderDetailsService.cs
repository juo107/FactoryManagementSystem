using System.Collections.Generic;
using System.Threading.Tasks;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.ProductionOrders;

namespace FactoryManagementSystem.Interfaces
{
    public interface IProductionOrderDetailsService
    {
        Task<ApiResponse<IEnumerable<BatchDto>>> GetBatchesAsync(int productionOrderId);
        Task<ApiResponse<object>> GetIngredientsByProductAsync(string productionOrderNumber);
        Task<ApiResponse<object>> GetMaterialConsumptionsAsync(string productionOrderNumber, int page, int limit);
        Task<ApiResponse<object>> GetMaterialConsumptionsExcludeBatchesAsync(string productionOrderNumber, int page, int limit, List<dynamic>? batchCodesWithMaterials);
        Task<ApiResponse<object>> GetBatchCodesWithMaterialsAsync(string productionOrderNumber);
        Task<ApiResponse<object>> GetRecipeVersionsAsync(string recipeCode, string? version);
        Task<ApiResponse<ProductionOrderDto>> GetByIdAsync(int id);
    }
}
