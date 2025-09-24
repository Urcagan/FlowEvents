using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IConnectionStringProvider _connectionProvider;

        public RoleRepository(IConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;  
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            var roles = new List<Role>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT RoleId, RoleName, description 
                    FROM Roles 
                    ORDER BY RoleName";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var role = new Role
                        {
                            RoleId = reader.GetInt32(0),
                            RoleName = reader.GetString(1)
                        };

                        if (!reader.IsDBNull(2)) role.Description = reader.GetString(2);

                        roles.Add(role);
                    }
                }
            }

            return roles;
        }

        
        public async Task<bool> RoleExistsAsync(int roleId)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(1) FROM Roles WHERE RoleId = @RoleId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RoleId", roleId);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }


    }
}
