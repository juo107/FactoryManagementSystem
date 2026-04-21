using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FactoryManagementSystem.Services
{
    public interface IMaterialsService
    {
        Task<object> GetProductionOrdersAsync(IQueryCollection query);
        Task<object> GetBatchCodesAsync(string? productionOrderNumber, IQueryCollection query);
        Task<object> GetIngredientsAsync(string? productionOrderNumber, string? batchCode, IQueryCollection query);
        Task<object> GetShiftsAsync(IQueryCollection query);
        Task<object> SearchAsync(IQueryCollection query, int page, int pageSize);
        Task<object> GetStatsSearchAsync(IQueryCollection query);
    }
}
