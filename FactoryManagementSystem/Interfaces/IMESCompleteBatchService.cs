using FactoryManagementSystem.DTOs;
using System.Threading.Tasks;

namespace FactoryManagementSystem.Interfaces
{
    public interface IMESCompleteBatchService
    {
        Task<MESCompleteBatchResponse> SearchAsync(MESCompleteBatchSearchParams paramsDto);
        Task<IEnumerable<string>> GetUniqueValuesAsync(string column);
    }
}
