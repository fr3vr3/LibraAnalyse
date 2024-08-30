using ClickHouse.Client.ADO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;  // Import for DbDataReader
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LibraAnalyse.Services
{
    public class ClickHouseService
    {
        private readonly string _connectionString;
        private readonly ILogger<ClickHouseService> _logger;

        public ClickHouseService(string connectionString, ILogger<ClickHouseService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(DataTable DataTable, string[] Logs)> ExecuteQueryAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));

            var logList = new List<string>();

            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Connection to ClickHouse opened successfully.");

                using var command = connection.CreateCommand();
                command.CommandText = query;
                _logger.LogInformation($"Executing query: {query}");

                using var reader = await command.ExecuteReaderAsync();
                var dbReader = reader as DbDataReader;

                if (dbReader == null)
                {
                    throw new InvalidOperationException("Failed to cast IDataReader to DbDataReader.");
                }

                return await ProcessReaderAsync(dbReader, query, logList);
            }
            catch (ClickHouse.Client.ClickHouseServerException serverEx)
            {
                var message = $"Server error during query execution: {serverEx.Message}";
                _logger.LogError(serverEx, message);
                logList.Add(message);
                throw new Exception(string.Join(Environment.NewLine, logList), serverEx);
            }
            catch (Exception ex)
            {
                var message = $"General error during query execution: {ex.Message}";
                _logger.LogError(ex, message);
                logList.Add(message);
                throw new Exception(string.Join(Environment.NewLine, logList), ex);
            }
        }

        private async Task<(DataTable DataTable, string[] Logs)> ProcessReaderAsync(DbDataReader reader, string query, List<string> logList)
        {
            if (!reader.HasRows)
            {
                logList.Add("Query executed successfully, but no rows were returned.");
                logList.Add($"Query: {query}");
                _logger.LogWarning("Query executed but returned no rows.");
                return (new DataTable(), logList.ToArray());
            }

            var dataTable = new DataTable();

            try
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    dataTable.Columns.Add(reader.GetName(i), typeof(string));
                }

                while (await reader.ReadAsync())
                {
                    var row = dataTable.NewRow();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.GetValue(i);
                        row[i] = value switch
                        {
                            Array arrayValue => string.Join(", ", arrayValue.Cast<object>()),
                            _ => value?.ToString()
                        };
                    }

                    dataTable.Rows.Add(row);
                }

                logList.Add("Query executed successfully with results.");
                _logger.LogInformation("Query executed successfully and data processed.");
                return (dataTable, logList.ToArray());
            }
            catch (Exception ex)
            {
                var message = $"Error during data processing: {ex.Message}";
                _logger.LogError(ex, message);
                logList.Add(message);
                throw new Exception(string.Join(Environment.NewLine, logList), ex);
            }
        }
    }
}
