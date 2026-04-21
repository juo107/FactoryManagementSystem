using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Interfaces;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Materials;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/production-materials")]
    public class MaterialsController : ControllerBase
    {
        private readonly IMaterialsService _service;

        public MaterialsController(IMaterialsService service)
        {
            _service = service;
        }

        [HttpGet("production-orders")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MaterialProductionOrderDto>>>> GetProductionOrders()
        {
            try
            {
                var result = await _service.GetProductionOrdersAsync(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("batch-codes")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MaterialBatchDto>>>> GetBatchCodes(string? productionOrderNumber = "")
        {
            try
            {
                var result = await _service.GetBatchCodesAsync(productionOrderNumber, Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("ingredients")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MaterialIngredientDto>>>> GetIngredients(string? productionOrderNumber, string? batchCode)
        {
            try
            {
                var result = await _service.GetIngredientsAsync(productionOrderNumber, batchCode, Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("shifts")]
        public async Task<ActionResult<ApiResponse<IEnumerable<MaterialSimpleDto>>>> GetShifts()
        {
            try
            {
                var result = await _service.GetShiftsAsync(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResponse<MaterialConsumptionDto>>>> Search(int page = 1, int pageSize = 100)
        {
            try
            {
                var result = await _service.SearchAsync(Request.Query, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("stats/search")]
        public async Task<ActionResult<ApiResponse<object>>> StatsSearch()
        {
            try
            {
                var result = await _service.GetStatsSearchAsync(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }
    }
}
