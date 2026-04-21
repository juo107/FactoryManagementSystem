using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.ProductionOrders;

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
        public async Task<ActionResult<ApiResponse<OrderFiltersDto>>> Filters([FromQuery] string dateFrom = "", [FromQuery] string dateTo = "")
        {
            try
            {
                var result = await _service.GetFiltersAsync(dateFrom, dateTo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("filters-v2")]
        public async Task<ActionResult<ApiResponse<OrderFiltersDto>>> FiltersV2([FromQuery] string dateFrom = "", [FromQuery] string dateTo = "")
        {
            try
            {
                var result = await _service.GetFiltersV2Async(dateFrom, dateTo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("stats/search")]
        public async Task<ActionResult<ApiResponse<OrderStatsDto>>> StatsSearch(
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
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("stats-v2/search")]
        public async Task<ActionResult<ApiResponse<OrderStatsDto>>> StatsSearchV2(
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
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResponse<ProductionOrderDto>>>> Search(
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
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("search-v2")]
        public async Task<ActionResult<ApiResponse<PagedResponse<ProductionOrderDto>>>> SearchV2(
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
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }
    }
}
