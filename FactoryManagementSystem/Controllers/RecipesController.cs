using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Recipes;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/production-recipes")]
    public class RecipesController : ControllerBase
    {
        private readonly IRecipesService _service;

        public RecipesController(IRecipesService service)
        {
            _service = service;
        }

        [HttpGet("stats/search")]
        public async Task<ActionResult<ApiResponse<object>>> GetStatsSearch(string? search, string? status, string? statuses)
        {
            try
            {
                var result = await _service.GetStatsSearchAsync(search, status, statuses);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResponse<RecipeDto>>>> Search(int page = 1, int limit = 20, string? search = null, string? status = null, string? statuses = null)
        {
            try
            {
                var result = await _service.SearchAsync(page, limit, search, status, statuses);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }
    }
}
