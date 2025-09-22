using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class DatabaseInfoRepository : IDatabaseInfoRepository
    {
        private string _connectionString;

        public DatabaseInfoRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<string> GetDatabaseVersionAsync(string connectionString)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    const string query = "SELECT Value FROM Config WHERE Parameter = 'VersionDB'";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CheckDatabaseStructureAsync(string connectionString)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Проверяем наличие основных таблиц
                    var tables = new[] { "Config", "Users", "Units", "Events" };

                    foreach (var table in tables)
                    {
                        var query = $"SELECT 1 FROM sqlite_master WHERE type='table' AND name='{table}'";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            var result = await command.ExecuteScalarAsync();
                            if (result == null) return false;
                        }
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    connection.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

}
