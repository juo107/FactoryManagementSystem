using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.DTOs.Materials;
using FactoryManagementSystem.DTOs.ProductionOrders;
using FactoryManagementSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<ApiResponse<IEnumerable<BatchDto>>>> GetBatches(int productionOrderId)
        {
            if (productionOrderId <= 0) return BadRequest(ApiResponse<object>.Error("ID không hợp lệ"));
            try
            {
                var result = await _service.GetBatchesAsync(productionOrderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("ingredients-by-product")]
        public async Task<ActionResult<ApiResponse<object>>> GetIngredients(string productionOrderNumber)
        {
            if (string.IsNullOrWhiteSpace(productionOrderNumber)) return BadRequest(ApiResponse<object>.Error("Số đơn hàng bắt buộc"));
            try
            {
                var result = await _service.GetIngredientsByProductAsync(productionOrderNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpPost("material-consumptions")]
        public async Task<ActionResult<ApiResponse<object>>> GetMaterialConsumptions(
            [FromQuery] string productionOrderNumber,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productionOrderNumber)) return BadRequest(ApiResponse<object>.Error("productionOrderNumber là bắt buộc"));
                var result = await _service.GetMaterialConsumptionsAsync(productionOrderNumber, page, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpPost("material-consumptions-exclude-batches")]
        public async Task<ActionResult<ApiResponse<object>>> GetExclude(
            [FromQuery] string productionOrderNumber,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromBody] List<BatchFilterDto>? batchCodesWithMaterials = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productionOrderNumber))
                    return BadRequest(ApiResponse<object>.Error("productionOrderNumber là bắt buộc"));

                var result = await _service.GetMaterialConsumptionsExcludeBatchesAsync(productionOrderNumber, page, limit, batchCodesWithMaterials);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("batch-codes-with-materials")]
        public async Task<ActionResult<ApiResponse<object>>> GetBatchCodes(string productionOrderNumber)
        {
            try
            {
                var result = await _service.GetBatchCodesWithMaterialsAsync(productionOrderNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("recipe-versions")]
        public async Task<ActionResult<ApiResponse<object>>> GetRecipe(string recipeCode, string? version)
        {
            try
            {
                var result = await _service.GetRecipeVersionsAsync(recipeCode, version);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductionOrderDto>>> GetById(int id)
        {
            if (id <= 0) return BadRequest(ApiResponse<object>.Error("ID đơn hàng không hợp lệ"));
            try
            {
                var result = await _service.GetByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }
    }
}
