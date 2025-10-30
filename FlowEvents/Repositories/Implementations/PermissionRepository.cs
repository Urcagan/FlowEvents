using DocumentFormat.OpenXml.Spreadsheet;
using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace FlowEvents.Repositories.Implementations
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly IConnectionStringProvider _connectionProvider;
        public PermissionRepository(IConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }


        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения
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

        public async Task<List<Permission>> GetPermissionsByRoleIdAsync(int roleId) // Получение прав пользователя по ID пользователя 
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения
            List<Permission> permissions = new List<Permission>();

            string query = $" Select r.RoleId, r.PermissionId , p.PermissionName " +
                                        "FROM RolePermissions r " +
                                        "JOIN Permissions p ON r.PermissionId = p.PermissionId " + 
                                        "Where r.RoleId = @RoleID";

            var parameters = new SQLiteParameter();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@RoleID", $"{roleId}");
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var permit = new Permission
                        {
                            PermissionId= reader.GetInt32(1),
                            PermissionName = reader.GetString(2),
                        };
                        permissions.Add(permit);
                    }
                }
            }
            return permissions;
        }


    }
}
