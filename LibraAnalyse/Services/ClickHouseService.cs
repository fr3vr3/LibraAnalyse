using ClickHouse.Client.ADO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace LibraAnalyse.Services
{
    public class ClickHouseService
    {
        private readonly string _connectionString;

        public ClickHouseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<(DataTable DataTable, string[] Logs)> ExecuteQueryAsync(string query)
        {
            var logList = new List<string>();
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                using var command = connection.CreateCommand();
                await connection.OpenAsync();
                command.CommandText = query;
                using var reader = await command.ExecuteReaderAsync();

                // Log if rows are found
                if (reader.HasRows)
                {
                    logList.Add("Rows found:");
                }
                else
                {
                    logList.Add("Query executed successfully, but no rows were returned.");
                    return (new DataTable(), logList.ToArray()); // Return an empty DataTable with logs
                }

                // Load data into DataTable the orthodox way
                var dataTable = new DataTable();
                try
                {
                    // Ensure schema is set correctly before loading data
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnType = reader.GetFieldType(i);
                        if (columnType == null)
                        {
                            columnType = typeof(string); // Fallback to string if type is unknown
                        }
                        dataTable.Columns.Add(reader.GetName(i), columnType);
                    }
                    dataTable.Load(reader);
                }
                catch (InvalidCastException ex)
                {
                    logList.Add($"InvalidCastException during DataTable.Load: {ex.Message}");
                    throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), ex); // Include logs in exception
                }
                catch (Exception ex)
                {
                    logList.Add($"General error during DataTable.Load: {ex.Message}");
                    throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), ex); // Include logs in exception
                }

                if (dataTable.Rows.Count == 0)
                {
                    logList.Add("DataTable is empty after loading data.");
                }
                return (dataTable, logList.ToArray());
            }
            catch (ClickHouse.Client.ClickHouseServerException serverEx)
            {
                logList.Add($"Server Error: {serverEx.Message}");
                throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), serverEx); // Include logs in exception
            }
            catch (Exception ex)
            {
                logList.Add($"General Error: {ex.Message}");
                throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), ex); // Include logs in exception
            }
        }

        public async Task<DataTable> GetTableDescriptionAsync(string tableName)
        {
            var query = $"DESCRIBE TABLE {tableName}";
            var (dataTable, logs) = await ExecuteQueryAsync(query);
            return dataTable;
        }

        public async Task<DataTable> QueryByAddressAsync(string address)
        {
            // This is a simplified example; actual implementation depends on your database schema
            var query = $"SELECT * FROM YourTable WHERE Address = '{address}'";
            var (dataTable, logs) = await ExecuteQueryAsync(query);
            return dataTable;
        }
    }
}
