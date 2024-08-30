using Microsoft.AspNetCore.Mvc;
using LibraAnalyse.Services;
using System.Threading.Tasks;

namespace LibraAnalyse.Controllers
{
    public class ClickHouseController : Controller
    {
        private readonly ClickHouseService _clickHouseService;

        public ClickHouseController(ClickHouseService clickHouseService)
        {
            _clickHouseService = clickHouseService;
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteQuery(string query)
        {
            var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
            // Process the dataTable and logs as needed
            return View(dataTable);  // Or return it as JSON, etc.
        }
    }
}
