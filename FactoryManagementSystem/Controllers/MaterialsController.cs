using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Services;

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
        public async Task<IActionResult> GetProductionOrders()
        {
            try
            {
                var result = await _service.GetProductionOrdersAsync(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("batch-codes")]
        public async Task<IActionResult> GetBatchCodes(string? productionOrderNumber = "")
        {
            try
            {
                var result = await _service.GetBatchCodesAsync(productionOrderNumber, Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("ingredients")]
        public async Task<IActionResult> GetIngredients(string? productionOrderNumber, string? batchCode)
        {
            try
            {
                var result = await _service.GetIngredientsAsync(productionOrderNumber, batchCode, Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("shifts")]
        public async Task<IActionResult> GetShifts()
        {
            try
            {
                var result = await _service.GetShiftsAsync(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(int page = 1, int pageSize = 100)
        {
            try
            {
                var result = await _service.SearchAsync(Request.Query, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("stats/search")]
        public async Task<IActionResult> StatsSearch()
        {
            try
            {
                var result = await _service.GetStatsSearchAsync(Request.Query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
