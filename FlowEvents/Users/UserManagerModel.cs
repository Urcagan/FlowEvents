using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class UserManagerModel : INotifyPropertyChanged
    {
        #region
        public event PropertyChangedEventHandler PropertyChanged;

        private string _connectionString;
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                GetUsers();
            }
        }

        //Таблица пользователей
        private DataTable _usersTable;
        public DataTable UsersTable
        {
            get => _usersTable;
            set
            {
                _usersTable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsersTable)));
            }
        }

        //Таблица ролей пользователей
        private DataTable _rolesTable;
        public DataTable RolesTable
        {
            get => _rolesTable;
            set
            {
                _rolesTable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RolesTable)));
            }
        }

        private MainViewModel _mainViewModel;
        public MainViewModel MainViewModel
        {
            get { return _mainViewModel; }
            set
            {
                _mainViewModel = value;
            }
        }

        #endregion


        public UserManagerModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            // Загрузка данных из базы
            ConnectionString = _mainViewModel._connectionString;
            LoadRoles(); // Загружаем роли при создании модели
            GetUsers();  // Загружаем пользователей
        }


        public void LoadRoles()
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("SELECT RoleId, RoleName, Description FROM Roles", connection);

                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        RolesTable = dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}");
            }
        }

        // Метод для загрузки данных из таблицы Units
        private void GetUsers()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("SELECT * FROM Users", connection);

                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        UsersTable = dataTable;
                    }                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}");
            }
        }

        public void AddDomainUser(DomainUser domainUser, int roleId)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                    "INSERT OR REPLACE INTO Users " +
                    "(Username, DomainName, DisplayName, Email, RoleId) " +
                    "VALUES (@username, @domain, @displayName, @email, @roleId)",
                    connection);

                    command.Parameters.AddWithValue("@username", domainUser.Username);
                    command.Parameters.AddWithValue("@domain", domainUser.DomainName);
                    command.Parameters.AddWithValue("@displayName", domainUser.DisplayName);
                    command.Parameters.AddWithValue("@email", domainUser.Email);
                    command.Parameters.AddWithValue("@roleId", roleId);

                    command.ExecuteNonQuery();
                    GetUsers(); // Обновляем таблицу
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления пользователя: {ex.Message}");
            }
        }

        public void UpdateUserAccess(string username, bool isAllowed)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "UPDATE Users SET IsAllowed = @isAllowed WHERE Username = @username",
                        connection);
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@isAllowed", isAllowed ? 1 : 0);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления доступа: {ex.Message}");
            }
        }

        public void UpdateUserRole(string username, int SelRole)
        {
            try
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "UPDATE Users SET RoleId = @selRole WHERE Username = @username",
                        connection);
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@selRole", SelRole);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления роли пользователя: {ex.Message}");
            }
        }

    }
}
