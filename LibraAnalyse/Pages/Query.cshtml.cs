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

        public Dictionary<string, DataTable> TableDescriptions { get; private set; } = new Dictionary<string, DataTable>();

        public async Task<IActionResult> OnPostDescribeTablesAsync()
        {
            TableDescriptions = await DescribeTablesAsync(_clickHouseService);
            return Page();
        }

        private async Task<Dictionary<string, DataTable>> DescribeTablesAsync(ClickHouseService clickHouseService)
        {
            string[] tables = {
            "ancestry", "beneficiary_policy", "block_metadata_transaction", "boundary_status",
            "burn_counter", "burn_tracker", "coin_balance", "community_wallet",
            "consensus_reward", "donor_voice_registry", "epoch_fee_maker_registry", "event",
            "genesis_transaction", "ingested_files", "ingested_versions", "multi_action",
            "multisig_account_owners", "ol_swap_1h", "script", "slow_wallet",
            "slow_wallet_list", "state_checkpoint_transaction", "total_supply",
            "tower_list", "user_transaction", "vdf_difficulty"
        };

            var descriptions = new Dictionary<string, DataTable>();

            foreach (var table in tables)
            {
                string query = $"DESCRIBE TABLE {table}";
                var (dataTable, logs) = await clickHouseService.ExecuteQueryAsync(query);
                descriptions[table] = dataTable;
            }

            return descriptions;
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
