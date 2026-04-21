using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Services;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/production-products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsService _service;

        public ProductsController(IProductsService service)
        {
            _service = service;
        }

        [HttpGet("types")]
        public async Task<IActionResult> GetTypes()
        {
            try
            {
                var result = await _service.GetTypesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string? q = "",
            string? status = "",
            string? statuses = "",
            string? type = "",
            string? types = "",
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var result = await _service.SearchAsync(q, status, statuses, type, types, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("stats/search")]
        public async Task<IActionResult> StatsSearch(
            string? q = "",
            string? status = "",
            string? statuses = "",
            string? type = "",
            string? types = "")
        {
            try
            {
                var result = await _service.GetStatsSearchAsync(q, status, statuses, type, types);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null) return NotFound(new { error = "Not found" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
