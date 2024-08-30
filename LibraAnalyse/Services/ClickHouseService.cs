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
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = query;

                using var reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    logList.Add("Query executed successfully, but no rows were returned.");
                    logList.Add($"Query: {query}");
                    return (new DataTable(), logList.ToArray());
                }

                var originalDataTable = new DataTable();

                try
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        originalDataTable.Columns.Add(reader.GetName(i), typeof(string)); // Store everything as string
                    }

                    while (reader.Read())
                    {
                        var row = originalDataTable.NewRow();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.GetValue(i);
                            if (value is Array arrayValue)
                            {
                                row[i] = string.Join(", ", arrayValue.Cast<object>());  // Concatenate array elements
                            }
                            else
                            {
                                row[i] = value?.ToString();  // Default string conversion
                            }
                        }

                        originalDataTable.Rows.Add(row);
                    }

                    return (originalDataTable, logList.ToArray());
                }
                catch (Exception ex)
                {
                    logList.Add($"General error during DataTable.Load: {ex.Message}");
                    throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), ex);
                }
            }
            catch (ClickHouse.Client.ClickHouseServerException serverEx)
            {
                logList.Add($"Server Error: {serverEx.Message}");
                throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), serverEx);
            }
            catch (Exception ex)
            {
                logList.Add($"General Error: {ex.Message}");
                throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), ex);
            }
        }
    }
}
