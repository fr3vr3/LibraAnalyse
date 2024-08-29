using ClickHouse.Client.ADO;
using System;
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

        public async Task<DataTable> ExecuteQueryAsync(string query)
        {
            using var connection = new ClickHouseConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = await command.ExecuteReaderAsync();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            return dataTable;
        }
    }
}
