using System.Threading.Tasks;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Recipes;

namespace FactoryManagementSystem.Interfaces
{
    public interface IRecipesService
    {
        Task<ApiResponse<object>> GetStatsSearchAsync(string? search, string? status, string? statuses);
        Task<ApiResponse<PagedResponse<RecipeDto>>> SearchAsync(int page, int limit, string? search, string? status, string? statuses);
        Task<ApiResponse<RecipeDetailResponseDto>> GetByIdAsync(int id);
    }
}
