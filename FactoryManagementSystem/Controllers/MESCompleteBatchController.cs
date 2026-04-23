using FactoryManagementSystem.DTOs;
using FactoryManagementSystem.DTOs.Common;
using FactoryManagementSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FactoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/mes-complete-batch")]
    public class MESCompleteBatchController : ControllerBase
    {
        private readonly IMESCompleteBatchService _service;

        public MESCompleteBatchController(IMESCompleteBatchService service)
        {
            _service = service;
        }

        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<MESCompleteBatchResponse>>> Search([FromQuery] MESCompleteBatchSearchParams paramsDto)
        {
            try
            {
                var result = await _service.SearchAsync(paramsDto);
                return Ok(ApiResponse<MESCompleteBatchResponse>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }
        [HttpGet("unique-values")]
        public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetUniqueValues([FromQuery] string column)
        {
            try
            {
                var result = await _service.GetUniqueValuesAsync(column);
                return Ok(ApiResponse<IEnumerable<string>>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Error(ex.Message));
            }
        }
    }
}
