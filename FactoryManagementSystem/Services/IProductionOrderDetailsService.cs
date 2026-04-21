using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FactoryManagementSystem.Services
{
    public interface IProductionOrderDetailsService
    {
        Task<object> GetBatchesAsync(int productionOrderId);
        Task<object> GetIngredientsByProductAsync(string productionOrderNumber);
        Task<object> GetMaterialConsumptionsAsync(string productionOrderNumber, int page, int limit);
        Task<object> GetMaterialConsumptionsExcludeBatchesAsync(string productionOrderNumber, int page, int limit, List<dynamic>? batchCodesWithMaterials);
        Task<object> GetBatchCodesWithMaterialsAsync(string productionOrderNumber);
        Task<object> GetRecipeVersionsAsync(string recipeCode, string? version);
        Task<object> GetByIdAsync(int id);
    }
}
