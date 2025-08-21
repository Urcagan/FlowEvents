using FlowEvents.Users;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FlowEvents
{
    public class UserManagerModel : INotifyPropertyChanged
    {

        public RelayCommand OpenFindUserWindowsCommand { get; set; }
        public RelayCommand DeletUserCommand { get; set; }
        public RelayCommand OpenAddUserWindowsCommand { get; set; }
        #region
        //public event PropertyChangedEventHandler PropertyChanged;

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


        // Коллекция для хранения категорий (источник данных (коллекцию))
        //public ObservableCollection<UnitModel> UsersTable { get; set; } = new ObservableCollection<UnitModel>();

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


        public UserManagerModel()
        {
            //_mainViewModel = mainViewModel;

            OpenFindUserWindowsCommand = new RelayCommand(OpenFindUserWindows);
            DeletUserCommand = new RelayCommand(DeletUser);
            OpenAddUserWindowsCommand = new RelayCommand(OpenAddUserWindows);

            // Загрузка данных из базы
            ConnectionString = Global_Var.ConnectionString; //_mainViewModel._connectionString;
            LoadRoles(); // Загружаем роли при создании модели
            GetUsers();  // Загружаем пользователей
        }

       
        private void OpenAddUserWindows(object parameters)
        {
            AddUserWindow addUserWindow = new AddUserWindow(this);

            void ClosedHandler(object sender, EventArgs e)
            {
                addUserWindow.Closed -= ClosedHandler; // Отвязываем
                GetUsers();  // Перезагружаем установки после закрытия окна UnitsView
            }

            addUserWindow.Closed += ClosedHandler; // Подписываемся на событие закрытия окна
            if (addUserWindow.ShowDialog() == true) { }
        }

        private void OpenFindUserWindows(object parameters)
        {

            //var findUserModel = new FindUserModel(_mainViewModel);
            FindUserWindow findUserWindow = new FindUserWindow(this); // Создаем дочернее окно, передавая текущую модель (this)

            void ClosedHandler(object sender, EventArgs e)
            {
                findUserWindow.Closed -= ClosedHandler; // Отвязываем
                GetUsers();  // Перезагружаем установки после закрытия окна UnitsView
            }

            findUserWindow.Closed += ClosedHandler; // Подписываемся на событие закрытия окна
            if (findUserWindow.ShowDialog() == true) { }

        }



        private DataRowView _selectedUser;
        public DataRowView SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }


        // Удаление категории
        private void DeletUser(object parameter)
        {
            if (SelectedUser == null) return;
            int selectRowID = Convert.ToInt32(SelectedUser.Row["id"]);

            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить пользователя {SelectedUser.Row["DisplayName"]} ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) { SelectedUser = null; return; }

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("DELETE FROM Users WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", selectRowID);
                    command.ExecuteNonQuery();
                }

                // Units.Remove(SelectedUser);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            SelectedUser = null;
            GetUsers();
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

        // Метод для загрузки данных из таблицы Users
        public void GetUsers()
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

        /**
        public void AddDomainUser(DomainUserModel domainUser, int roleId)
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
        **/

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

        // Реализация INotifyPropertyChanged
        // Событие, которое уведомляет об изменении свойства
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    

}
