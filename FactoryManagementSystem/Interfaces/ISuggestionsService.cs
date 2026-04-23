using FactoryManagementSystem.DTOs.Common;

namespace FactoryManagementSystem.Interfaces
{
    public interface ISuggestionsService
    {
        Task<ApiResponse<IEnumerable<SuggestionDto>>> GetSuggestionsAsync(string table, string column, string q);
    }
}
