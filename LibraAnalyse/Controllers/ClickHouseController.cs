using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LibraAnalyse.Services;

namespace LibraAnalyse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClickHouseController : ControllerBase
    {
        private readonly ClickHouseService _clickHouseService;

        public ClickHouseController(ClickHouseService clickHouseService)
        {
            _clickHouseService = clickHouseService;
        }

        [HttpGet("query")]
        public async Task<IActionResult> ExecuteQuery(string sqlQuery)
        {
            try
            {
                var result = await _clickHouseService.ExecuteQueryAsync(sqlQuery);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
