using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IConnectionStringProvider _connectionProvider;

        public UserRepository(IConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }


        // Асинхронный метод для получения всех пользователей из базы данных
        public async Task<List<User>> GetAllUsersAsync()
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            var users = new List<User>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT u.id, u.UserName, u.DomainName, u.DisplayName, u.Email, 
                           u.RoleId, u.IsAllowed, r.RoleName
                    FROM Users u
                    LEFT JOIN Roles r ON u.RoleId = r.RoleId
                    ORDER BY u.UserName";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var user = new User
                        {
                            Id = reader.GetInt32(0),
                            UserName = reader.GetString(1),
                            RoleId = reader.GetInt32(5),
                            IsAllowed = reader.GetInt32(6) == 1 ? 1 : 0
                        };

                        // Обрабатываем возможные NULL значения
                        if (!reader.IsDBNull(2)) user.DomainName = reader.GetString(2);

                        if (!reader.IsDBNull(3)) user.DisplayName = reader.GetString(3);

                        if (!reader.IsDBNull(4)) user.Email = reader.GetString(4);

                        users.Add(user);
                    }
                }
            }

            return users;
        }


        // Асинхронный метод для проверки существования пользователя по имени
        public async Task<bool> UserExistsAsync(string username)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE UserName = @username";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        // Асинхронный метод для добавления пользователя в БД и возврата его ID
        public async Task<int> AddUserAsync(string username, string hashedPassword, string salt, int roleId)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO Users 
                (UserName, DomainName, DisplayName, Email, RoleId, IsAllowed, Password, Salt) 
                VALUES 
                (@username, '', @username, '', @roleId, 1, @password, @salt);
                SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@roleId", roleId);
                    command.Parameters.AddWithValue("@password", hashedPassword);
                    command.Parameters.AddWithValue("@salt", salt);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        // Добавляем доменного пользователя
        public async Task<int> AddDomenUserAsync(string username, string domainName, string displayName, string email, int roleId)
        {
            var _connectionString = _connectionProvider.GetConnectionString();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO Users (UserName, DomainName, DisplayName, RoleId, Email, IsAllowed) " +
                        "VALUES (@UserName, @DomainName, @DisplayName, @RoleId, @Email, 1)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", username);
                    command.Parameters.AddWithValue("@DomainName", string.IsNullOrEmpty(domainName) ? DBNull.Value : (object)domainName);
                    command.Parameters.AddWithValue("@DisplayName", string.IsNullOrEmpty(displayName) ? DBNull.Value : (object)displayName);
                    command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : (object)email);
                    command.Parameters.AddWithValue("@RoleId", roleId);

                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }

        }

        public async Task<bool> DeleteUserAsync(string userName)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string query = "DELETE FROM Users WHERE UserName = @UserName";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", userName);

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task UpdateUserRoleAsync(string username, int newRoleId)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "UPDATE Users SET RoleId = @selRole WHERE Username = @username";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@selRole", newRoleId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        public async Task UpdateUserAccessAsync(string username, bool isAllowed)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string query = @"
                                    UPDATE Users 
                                    SET IsAllowed = @isAllowed 
                                    WHERE UserName = @username";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@isAllowed", isAllowed);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        throw new InvalidOperationException($"Пользователь '{username}' не найден");
                    }
                }
            }
        }


    }
}
