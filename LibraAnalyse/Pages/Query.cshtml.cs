using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LibraAnalyse.Services;
    
namespace LibraAnalyse.Pages
{
    public class QueryModel : PageModel
    {
        private readonly ClickHouseService _clickHouseService;

        public QueryModel(ClickHouseService clickHouseService)
        {
            _clickHouseService = clickHouseService;
        }

        // Properties to store results
        public List<string> TableNames { get; set; }
        public Dictionary<string, DataTable> TableDescriptions { get; set; }
        [BindProperty]
        public string TableName { get; set; }
        public string SelectedTableName { get; set; }
        public DataTable SelectedTableContent { get; set; }

        // Method to list all tables
        public async Task<IActionResult> OnPostListTablesAsync()
        {
            string query = "SHOW TABLES";
            var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
            TableNames = dataTable.AsEnumerable().Select(row => row[0].ToString()).ToList();
            return Page();
        }

        // Method to describe all tables
        public async Task<IActionResult> OnPostDescribeTablesAsync()
        {
            if (TableNames == null || !TableNames.Any())
            {
                await OnPostListTablesAsync();
            }

            TableDescriptions = new Dictionary<string, DataTable>();

            foreach (var table in TableNames)
            {
                string query = $"DESCRIBE TABLE {table}";
                var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
                TableDescriptions.Add(table, dataTable);
            }
            return Page();
        }

        // Method to get content of a specific table
        public async Task<IActionResult> OnPostGetTableContentAsync()
        {
            SelectedTableName = TableName;
            string query = $"SELECT * FROM {SelectedTableName} LIMIT 1000"; // Adjust limit as needed
            var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
            SelectedTableContent = dataTable;
            return Page();
        }
    }
}
