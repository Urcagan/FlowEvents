using FlowEvents.Repositories.Interface;
using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class DatabaseInfoRepository : IDatabaseInfoRepository
    {
        // Получить текущую версию
        public async Task<int> GetUserVersion(string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();
                using var cmd = new SQLiteCommand("PRAGMA user_version;", connection);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public async Task<bool> CheckDatabaseStructureAsync(string connectionString) // Проверка структуры базы данных
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Проверяем наличие основных таблиц
                    var tables = new[] { "AttachedFiles", "Category","EventUnits", "Events", "Permissions", "RolePermissions", "Roles", "Units", "Users",  };

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

        public async Task<bool> TestConnectionAsync(string connectionString) // Проверка подключения к базе данных
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
