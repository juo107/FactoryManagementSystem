using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Services;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/production-orders")]
    public class ProductionOrdersController : ControllerBase
    {
        private readonly IProductionOrdersService _service;

        public ProductionOrdersController(IProductionOrdersService service)
        {
            _service = service;
        }

        [HttpGet("filters")]
        public async Task<IActionResult> Filters([FromQuery] string dateFrom = "", [FromQuery] string dateTo = "")
        {
            try
            {
                var result = await _service.GetFiltersAsync(dateFrom, dateTo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("filters-v2")]
        public async Task<IActionResult> FiltersV2([FromQuery] string dateFrom = "", [FromQuery] string dateTo = "")
        {
            try
            {
                var result = await _service.GetFiltersV2Async(dateFrom, dateTo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("stats/search")]
        public async Task<IActionResult> StatsSearch(
            [FromQuery] string searchQuery = "",
            [FromQuery] string dateFrom = "",
            [FromQuery] string dateTo = "",
            [FromQuery] string processAreas = "",
            [FromQuery] string shifts = "",
            [FromQuery] string statuses = ""
        )
        {
            try
            {
                var result = await _service.GetStatsSearchAsync(searchQuery, dateFrom, dateTo, processAreas, shifts, statuses);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("stats-v2/search")]
        public async Task<IActionResult> StatsSearchV2(
            [FromQuery] string searchQuery = "",
            [FromQuery] string dateFrom = "",
            [FromQuery] string dateTo = "",
            [FromQuery] string processAreas = "",
            [FromQuery] string shifts = "",
            [FromQuery] string statuses = "",
            [FromQuery] string pos = "",
            [FromQuery] string batchIds = ""
        )
        {
            try
            {
                var result = await _service.GetStatsSearchV2Async(searchQuery, dateFrom, dateTo, processAreas, shifts, statuses, pos, batchIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? searchQuery = "",
            [FromQuery] string? dateFrom = "",
            [FromQuery] string? dateTo = "",
            [FromQuery] string? processAreas = "",
            [FromQuery] string? shifts = "",
            [FromQuery] string? statuses = "",
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] int total = 0
        )
        {
            try
            {
                var result = await _service.SearchAsync(searchQuery, dateFrom, dateTo, processAreas, shifts, statuses, page, limit, total);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("search-v2")]
        public async Task<IActionResult> SearchV2(
            [FromQuery] string? searchQuery = "",
            [FromQuery] string? dateFrom = "",
            [FromQuery] string? dateTo = "",
            [FromQuery] string? processAreas = "",
            [FromQuery] string? shifts = "",
            [FromQuery] string? statuses = "",
            [FromQuery] string? pos = "",
            [FromQuery] string? batchIds = "",
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] int total = 0
        )
        {
            try
            {
                var result = await _service.SearchV2Async(searchQuery, dateFrom, dateTo, processAreas, shifts, statuses, pos, batchIds, page, limit, total);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
