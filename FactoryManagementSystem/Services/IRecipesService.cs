using System.Threading.Tasks;

namespace FactoryManagementSystem.Services
{
    public interface IRecipesService
    {
        Task<object> GetStatsSearchAsync(string? search, string? status, string? statuses);
        Task<object> SearchAsync(int page, int limit, string? search, string? status, string? statuses);
        Task<object> GetByIdAsync(int id);
    }
}
