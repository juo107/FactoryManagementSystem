using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Services;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/production-recipe-detail")]
    public class RecipeDetailsController : ControllerBase
    {
        private readonly IRecipesService _service;

        public RecipeDetailsController(IRecipesService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null) return NotFound(new { success = false, message = "Recipe not found" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
