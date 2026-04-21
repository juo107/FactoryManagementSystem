using Microsoft.AspNetCore.Mvc;
using FactoryManagementSystem.Services;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/production-order-detail")]
    public class ProductionOrderDetailsController : ControllerBase
    {
        private readonly IProductionOrderDetailsService _service;

        public ProductionOrderDetailsController(IProductionOrderDetailsService service)
        {
            _service = service;
        }

        [HttpGet("batches")]
        public async Task<IActionResult> GetBatches(int productionOrderId)
        {
            if (productionOrderId <= 0) return BadRequest(new { success = false, message = "ID không hợp lệ" });
            try
            {
                var result = await _service.GetBatchesAsync(productionOrderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("ingredients-by-product")]
        public async Task<IActionResult> GetIngredients(string productionOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(productionOrderNumber)) return BadRequest(new { success = false });
            try
            {
                var result = await _service.GetIngredientsByProductAsync(productionOrderNumber);
                if (result == null) return NotFound(new { success = false });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("material-consumptions")]
        public async Task<IActionResult> GetMaterialConsumptions(
            [FromQuery] string productionOrderNumber,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productionOrderNumber)) return BadRequest(new { success = false, message = "productionOrderNumber là bắt buộc" });
                var result = await _service.GetMaterialConsumptionsAsync(productionOrderNumber, page, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }

        [HttpPost("material-consumptions-exclude-batches")]
        public async Task<IActionResult> GetExclude(
            [FromQuery] string productionOrderNumber,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromBody] List<dynamic>? batchCodesWithMaterials = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productionOrderNumber)) return BadRequest(new { success = false, message = "productionOrderNumber là bắt buộc" });
                var result = await _service.GetMaterialConsumptionsExcludeBatchesAsync(productionOrderNumber, page, limit, batchCodesWithMaterials);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }

        [HttpGet("batch-codes-with-materials")]
        public async Task<IActionResult> GetBatchCodes(string productionOrderNumber)
        {
            try
            {
                var result = await _service.GetBatchCodesWithMaterialsAsync(productionOrderNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("recipe-versions")]
        public async Task<IActionResult> GetRecipe(string recipeCode, string? version)
        {
            try
            {
                var result = await _service.GetRecipeVersionsAsync(recipeCode, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "ID đơn hàng không hợp lệ" });
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }
    }
}
