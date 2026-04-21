using System.Threading.Tasks;

namespace FactoryManagementSystem.Services
{
    public interface IProductsService
    {
        Task<object> GetTypesAsync();
        Task<object> SearchAsync(string? q, string? status, string? statuses, string? type, string? types, int page, int pageSize);
        Task<object> GetStatsSearchAsync(string? q, string? status, string? statuses, string? type, string? types);
        Task<object> GetByIdAsync(string id);
    }
}
