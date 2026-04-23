using FactoryManagementSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommonController : ControllerBase
    {
        private readonly ISuggestionsService _suggestionsService;

        public CommonController(ISuggestionsService suggestionsService)
        {
            _suggestionsService = suggestionsService;
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string table, [FromQuery] string column, [FromQuery] string q)
        {
            var result = await _suggestionsService.GetSuggestionsAsync(table, column, q);
            return Ok(result);
        }
    }
}
