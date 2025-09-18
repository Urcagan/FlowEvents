using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class PermissionRepository : IPermissionRepository
    {
        private string _connectionString;
        public PermissionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }


        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            var permissions = new List<Permission>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT PermissionID, PermissionName, Description, IsGranted
                    FROM Permissions";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var permission = new Permission
                        {
                            PermissionId = reader.GetInt32(0),
                            PermissionName = reader.GetString(1),
                           // Description = reader.GetString(2),
                            IsGrantedBool = reader.GetBoolean(3)                            
                        };

                        // Обрабатываем возможные NULL значения
                        if (!reader.IsDBNull(2)) permission.Description = reader.GetString(2);

                        permissions.Add(permission);
                    }
                }
            }

            return permissions;
        }

        public Task<List<Permission>> GetPermissionsByRoleIdAsync(int roleId)
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
