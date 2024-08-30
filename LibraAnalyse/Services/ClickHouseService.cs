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

                if (reader.HasRows)
                {
                    logList.Add("Rows found:");
                }
                else
                {
                    logList.Add("Query executed successfully, but no rows were returned.");
                    return (new DataTable(), logList.ToArray());
                }

                var dataTable = new DataTable();

                try
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnType = reader.GetFieldType(i);
                        if (columnType == null)
                        {
                            columnType = typeof(string);
                        }

                        dataTable.Columns.Add(reader.GetName(i), columnType);
                    }

                    dataTable.Load(reader);
                }
                catch (InvalidCastException ex)
                {
                    logList.Add($"InvalidCastException during DataTable.Load: {ex.Message}");
                    throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), ex);
                }
                catch (Exception ex)
                {
                    logList.Add($"General error during DataTable.Load: {ex.Message}");
                    throw new Exception(string.Join(Environment.NewLine, logList.ToArray()), ex);
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
