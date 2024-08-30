using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
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

        private static readonly string[] Tables =
        {
            "ancestry", "beneficiary_policy", "block_metadata_transaction",
            "boundary_status", "burn_counter", "burn_tracker", "coin_balance",
            "community_wallet", "consensus_reward", "donor_voice_registry",
            "epoch_fee_maker_registry", "event", "genesis_transaction",
            "ingested_files", "ingested_versions", "multi_action",
            "multisig_account_owners", "ol_swap_1h", "script", "slow_wallet",
            "slow_wallet_list", "state_checkpoint_transaction", "total_supply",
            "tower_list", "user_transaction", "vdf_difficulty"
        };

        public List<string> TableNames { get; set; }
        public Dictionary<string, DataTable> TableDescriptions { get; set; }
        [BindProperty]
        public string TableName { get; set; }
        public string SelectedTableName { get; set; }
        public DataTable SelectedTableContent { get; set; }
        public DataTable CustomQueryResult { get; private set; }
        public int ResultCount { get; private set; }
        public Dictionary<string, DataTable> SearchResults { get; private set; }

        public async Task<IActionResult> OnPostListTablesAsync()
        {
            string query = "SHOW TABLES";
            var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
            TableNames = dataTable.AsEnumerable().Select(row => row[0].ToString()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostDescribeTablesAsync()
        {
            if (TableNames == null || !TableNames.Any())
            {
                await OnPostListTablesAsync();
            }

            TableDescriptions = new Dictionary<string, DataTable>();

            foreach (var table in TableNames)
            {
                string query = $"DESCRIBE TABLE {table} LIMIT 100";
                var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
                TableDescriptions.Add(table, dataTable);
            }
            return Page();
        }

        public async Task<IActionResult> OnPostGetTableContentAsync()
        {
            SelectedTableName = TableName;
            string query = $"SELECT * FROM {SelectedTableName} LIMIT 100";
            var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
            SelectedTableContent = dataTable;
            return Page();
        }

        public async Task<IActionResult> OnPostRunCustomQueryAsync(string customQuery)
        {
            if (string.IsNullOrWhiteSpace(customQuery))
            {
                ModelState.AddModelError("", "Query cannot be empty.");
                return Page();
            }

            try
            {
                var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(customQuery);
                CustomQueryResult = dataTable;

                var countQuery = "SELECT COUNT(DISTINCT account_address) AS TotalCount FROM event;";
                var (countTable, _) = await _clickHouseService.ExecuteQueryAsync(countQuery);
                if (countTable.Rows.Count > 0)
                {
                    ResultCount = Convert.ToInt32(countTable.Rows[0]["TotalCount"]);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error executing query: {ex.Message}");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSearchAddressAsync(string searchAddress)
        {
            if (string.IsNullOrWhiteSpace(searchAddress))
            {
                ModelState.AddModelError("", "Address cannot be empty.");
                return Page();
            }

            SearchResults = new Dictionary<string, DataTable>();
            var logList = new List<string>();

            string[] columnsToSearch = { "account_address", "address", "sender" };

            try
            {
                // Convert the input address from hex to BigInteger (remove 0x if present)
                string addressForQuery = searchAddress.StartsWith("0x")
                    ? searchAddress.Substring(2)
                    : searchAddress;

                // BigInteger addressAsBigInt;
                // try
                // {
                //     addressAsBigInt = BigInteger.Parse(addressForQuery, System.Globalization.NumberStyles.HexNumber);
                // }
                // catch (FormatException ex)
                // {
                //     ModelState.AddModelError("", $"Invalid address format: {ex.Message}");
                //     return Page();
                // }

                foreach (var table in Tables)
                {
                    string describeQuery = $"DESCRIBE TABLE {table}";
                    var (describeTable, _) = await _clickHouseService.ExecuteQueryAsync(describeQuery);
                    var existingColumns = describeTable.AsEnumerable().Select(row => row["name"].ToString()).ToHashSet();

                    var validColumnsToSearch = columnsToSearch.Where(col => existingColumns.Contains(col)).ToArray();
                    if (!validColumnsToSearch.Any())
                    {
                        continue;
                    }

                    string query = $"SELECT * FROM {table} WHERE " +
                                   string.Join(" OR ", validColumnsToSearch.Select(col => $"{col} = '{searchAddress}'"));

                    var (dataTable, logs) = await _clickHouseService.ExecuteQueryAsync(query);
                    if (dataTable.Rows.Count > 0)
                    {
                        SearchResults.Add(table, dataTable);
                    }
                    logList.AddRange(logs);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error executing search: {ex.Message}");
            }

            return Page();
        }
    }
}
