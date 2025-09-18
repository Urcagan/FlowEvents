using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class RoleRepository : IRoleRepository
    {
        private string _connectionString;

        public RoleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
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

        public Task<Role> GetRoleByIdAsync(int id)
        {
            throw new NotImplementedException();
        }


        //-------------------------------------------------------------------
        /// <summary>
        /// Метод для обновления строки подключения во время работы приложения
        /// </summary>
        /// <param name="newConnectionString"> Строка с нового подключения </param>
        //-------------------------------------------------------------------
        public void UpdateConnectionString(string newConnectionString)
        {
            _connectionString = newConnectionString;
        }
    }
}
