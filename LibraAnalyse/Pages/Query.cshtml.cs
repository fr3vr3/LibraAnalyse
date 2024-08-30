using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LibraAnalyse.Services;
using System.Numerics;

namespace LibraAnalyse.Pages
{
    public class QueryModel : PageModel
    {
        #region Fields and Constructor

        private readonly ClickHouseService _clickHouseService;

        public QueryModel(ClickHouseService clickHouseService)
        {
            _clickHouseService = clickHouseService;
        }

        #endregion

        #region Static Table Definitions

        private static readonly string[] Tables =
        {
            "ancestry", "beneficiary_policy", "block_metadata_transaction",
            "boundary_status", "burn_counter", "burn_tracker", "coin_balance",
            "community_wallet", "consensus_reward", "epoch_fee_maker_registry",
            "event", "genesis_transaction", "ingested_files", "ingested_versions",
            "multi_action", "multisig_account_owners", "ol_swap_1h", "script",
            "slow_wallet", "slow_wallet_list", "state_checkpoint_transaction",
            "total_supply", "tower_list", "user_transaction", "vdf_difficulty"
        };

        #endregion

        #region Properties

        public List<string> TableNames { get; set; }
        public Dictionary<string, DataTable> TableDescriptions { get; set; }
        public string SelectedTableName { get; set; }
        public DataTable SelectedTableContent { get; set; }
        public DataTable CustomQueryResult { get; private set; }
        public int ResultCount { get; private set; }
        public Dictionary<string, DataTable> SearchResults { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Lists all available tables in the database.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostListTablesAsync()
        {
            try
            {
                string query = "SHOW TABLES";
                var (dataTable, _) = await _clickHouseService.ExecuteQueryAsync(query);
                TableNames = dataTable.AsEnumerable().Select(row => row[0].ToString()).ToList();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error listing tables: {ex.Message}");
            }

            return Page();
        }

        /// <summary>
        /// Describes the structure of each table listed.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostDescribeTablesAsync()
        {
            try
            {
                if (TableNames == null || !TableNames.Any())
                {
                    await OnPostListTablesAsync();
                }

                TableDescriptions = new Dictionary<string, DataTable>();

                foreach (var table in TableNames)
                {
                    string query = $"DESCRIBE TABLE {table}";
                    var (dataTable, _) = await _clickHouseService.ExecuteQueryAsync(query);
                    TableDescriptions.Add(table, dataTable);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error describing tables: {ex.Message}");
            }

            return Page();
        }

        /// <summary>
        /// Retrieves the content of a specified table, limited to 100 rows.
        /// </summary>
        /// <param name="tableName">Name of the table to retrieve content from.</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostGetTableContentAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                ModelState.AddModelError("", "Table name cannot be empty.");
                return Page();
            }

            try
            {
                SelectedTableName = tableName;
                string query = $"SELECT * FROM {SelectedTableName} LIMIT 100";
                var (dataTable, _) = await _clickHouseService.ExecuteQueryAsync(query);
                SelectedTableContent = dataTable;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error getting table content: {ex.Message}");
            }

            return Page();
        }

        /// <summary>
        /// Executes a custom SQL query and returns the results.
        /// </summary>
        /// <param name="customQuery">The custom SQL query to execute.</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostRunCustomQueryAsync(string customQuery)
        {
            if (string.IsNullOrWhiteSpace(customQuery))
            {
                ModelState.AddModelError("", "Query cannot be empty.");
                return Page();
            }

            try
            {
                var (dataTable, _) = await _clickHouseService.ExecuteQueryAsync(customQuery);
                CustomQueryResult = dataTable;

                // Calculate the distinct count of account addresses as an additional query.
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

        /// <summary>
        /// Searches for a specific address across all relevant tables.
        /// </summary>
        /// <param name="searchAddress">The address to search for.</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostSearchAddressAsync(string searchAddress)
        {
            if (string.IsNullOrWhiteSpace(searchAddress))
            {
                ModelState.AddModelError("", "Address cannot be empty.");
                return Page();
            }

            try
            {
                SearchResults = new Dictionary<string, DataTable>();

                // Convert the address to a BigInteger if it contains any letters (indicating it's hexadecimal).
                string addressForQuery = searchAddress.Any(c => char.IsLetter(c))
                    ? BigInteger.Parse("0" + searchAddress, System.Globalization.NumberStyles.HexNumber).ToString()
                    : searchAddress;

                foreach (var table in Tables)
                {
                    string describeQuery = $"DESCRIBE TABLE {table}";
                    var (describeTable, _) = await _clickHouseService.ExecuteQueryAsync(describeQuery);
                    var existingColumns = describeTable.AsEnumerable().Select(row => row["name"].ToString()).ToHashSet();

                    var columnsToSearch = new[] { "account_address", "address", "sender" };
                    var validColumnsToSearch = columnsToSearch.Where(col => existingColumns.Contains(col)).ToArray();
                    if (!validColumnsToSearch.Any()) continue;

                    string query = $"SELECT * FROM {table} WHERE " +
                                   string.Join(" OR ", validColumnsToSearch.Select(col => $"{col} = '{addressForQuery}'"));

                    var (dataTable, _) = await _clickHouseService.ExecuteQueryAsync(query);
                    if (dataTable.Rows.Count > 0)
                    {
                        SearchResults.Add(table, dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error executing search: {ex.Message}");
            }

            return Page();
        }

        #endregion
    }
}
