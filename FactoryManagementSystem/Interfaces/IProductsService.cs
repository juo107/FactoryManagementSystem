using System.Collections.Generic;
using System.Threading.Tasks;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Products;

namespace FactoryManagementSystem.Interfaces
{
    public interface IProductsService
    {
        Task<ApiResponse<IEnumerable<string>>> GetTypesAsync();
        Task<ApiResponse<PagedResponse<ProductDto>>> SearchAsync(string? q, string? status, string? statuses, string? type, string? types, int page, int pageSize);
        Task<ApiResponse<object>> GetStatsSearchAsync(string? q, string? status, string? statuses, string? type, string? types);
        Task<ApiResponse<ProductDto>> GetByIdAsync(string id);
    }
}
