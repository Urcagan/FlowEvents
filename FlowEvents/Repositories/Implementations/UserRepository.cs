using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents.Repositories
{
    public class UserRepository : IUserRepository
    {
        private string _connectionString;

        public UserRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        

        public async Task<List<User>> GetAllUsersAsync()
        {
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



        // Асинхронный метод для добавления пользователя в БД
        public async Task AddLocalUserToDatabaseAsync(string username, string hashedPassword, string salt, int RoleId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO Users 
                    (UserName, DomainName, DisplayName, Email, RoleId, IsAllowed, Password, Salt) 
                    VALUES 
                    (@username, '', @username, '', @roleId, 1, @password, @salt)";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@roleId", RoleId);
                    command.Parameters.AddWithValue("@password", hashedPassword);
                    command.Parameters.AddWithValue("@salt", salt);

                    await command.ExecuteNonQueryAsync();
                }
            }
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
