using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using LibraAnalyse.Services;
using System.Data;

namespace LibraAnalyse.Pages
{
    public class QueryModel : PageModel
    {
        private readonly ClickHouseService _clickHouseService;

        public QueryModel(ClickHouseService clickHouseService)
        {
            _clickHouseService = clickHouseService;
        }

        [BindProperty]
        public string SqlQuery { get; set; }

        public DataTable QueryResult { get; private set; }

        // Property to hold error messages
        public string ErrorMessage { get; private set; }

        // Property to hold logs from the ClickHouseService
        public string[] Logs { get; private set; }

        public void OnGet()
        {
            // Initialize page or perform any setup needed
        }

        public async Task<IActionResult> OnPostListTablesAsync()
        {
            SqlQuery = "SHOW TABLES";
            return await ExecuteQueryAsync();
        }

        public async Task<IActionResult> OnPostDescribeTableAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                ErrorMessage = "Table name cannot be empty.";
                return Page();
            }

            SqlQuery = $"DESCRIBE TABLE {tableName}";
            return await ExecuteQueryAsync();
        }

        public async Task<IActionResult> OnPostExecuteQueryAsync()
        {
            if (string.IsNullOrWhiteSpace(SqlQuery))
            {
                ErrorMessage = "Query cannot be empty.";
                return Page();
            }

            return await ExecuteQueryAsync();
        }

        private async Task<IActionResult> ExecuteQueryAsync()
        {
            try
            {
                // Call the ClickHouseService and capture both the DataTable and Logs
                var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(SqlQuery);

                QueryResult = dataTable;
                Logs = logs;

                // Check if the DataTable is empty and set an appropriate error message
                if (QueryResult.Rows.Count == 0)
                {
                    ErrorMessage = "The query returned no results.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Error executing query: {ex.Message}";
            }

            return Page();
        }
    }
}
