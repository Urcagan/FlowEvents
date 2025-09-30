using FlowEvents.Services.Interface;
using FlowEvents.Users;
using FlowEvents.ViewModels;
using FlowEvents.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class UserManagerModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;

        private string _connectionString;
        private DataTable _usersTable;//Таблица пользователей
        private DataTable _rolesTable;//Таблица ролей пользователей

        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                GetUsers();
            }
        }

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

        public DataTable RolesTable
        {
            get => _rolesTable;
            set
            {
                _rolesTable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RolesTable)));
            }
        }
        //public RelayCommand OpenFindUserWindowCommand { get; set; }
        public RelayCommand DeletUserCommand { get; set; }
      //  public RelayCommand OpenAddUserWindowCommand { get; set; }
      //  public RelayCommand OpenPermissionWindowCommand { get; set; }

      //  public RelayCommand UserSearchWindowCommand { get; set; }  
        #region


        #endregion

        public UserManagerModel(IUserService userService)
        {
            _userService = userService;

            //OpenFindUserWindowCommand = new RelayCommand(OpenFindUserWindows);
            DeletUserCommand = new RelayCommand(DeletUser);
            //OpenAddUserWindowCommand = new RelayCommand(OpenAddUserWindows);
            //OpenPermissionWindowCommand = new RelayCommand(OpenPermissionWindow);

          //  UserSearchWindowCommand = new RelayCommand(UserSearchWindow);

            // Загрузка данных из базы
            ConnectionString = Global_Var.ConnectionString;
            LoadRoles(); // Загружаем роли при создании модели
            GetUsers();  // Загружаем пользователей
        }

        //private void OpenPermissionWindow(object parametrs)
        //{
        //    var PermissionViewModel = App.ServiceProvider.GetRequiredService<PermissionViewModel>(); // 1. Берем ViewModel из контейнера
        //    var PermissionWindow = new PermissionWindow();  // 2. Создаем окно обычным способом
        //    PermissionWindow.DataContext = PermissionViewModel; // 3. Связываем ViewModel с окном 
        //    PermissionWindow.Owner = Application.Current.MainWindow;
        //    PermissionWindow.ShowDialog(); // 5. Показываем модально
        //}

        //private void OpenAddUserWindows()
        //{
        //    var addUserViewModel = App.ServiceProvider.GetRequiredService<AddUserViewModel>();
        //    var addUserWindow = new AddUserWindow();
        //    addUserWindow.DataContext = addUserViewModel;
        //    addUserWindow.Owner = Application.Current.MainWindow;

        //    void ClosedHandler(object sender, EventArgs e)
        //    {
        //        addUserWindow.Closed -= ClosedHandler; // Отвязываем
        //        GetUsers();  // Перезагружаем установки после закрытия окна UnitsView
        //    }
        //    addUserWindow.Closed += ClosedHandler; // Подписываемся на событие закрытия окна
        //    if (addUserWindow.ShowDialog() == true) { }
        //}

        //private void OpenFindUserWindows(object parameters)
        //{
        //    FindUserWindow findUserWindow = new FindUserWindow(this); // Создаем дочернее окно, передавая текущую модель (this)

        //    void ClosedHandler(object sender, EventArgs e)
        //    {
        //        findUserWindow.Closed -= ClosedHandler; // Отвязываем
        //        GetUsers();  // Перезагружаем установки после закрытия окна UnitsView
        //    }

        //    findUserWindow.Closed += ClosedHandler; // Подписываемся на событие закрытия окна
        //    if (findUserWindow.ShowDialog() == true) { }

        //}

        //private void UserSearchWindow ()
        //{
        //    var UserDomainSearchViewModel = App.ServiceProvider.GetRequiredService<UserDomainSearchViewModel>();

        //    var UserDomainSearch = new UserDomainSearch();

        //    UserDomainSearch.DataContext = UserDomainSearchViewModel;

        //    UserDomainSearch.Owner = Application.Current.MainWindow;
        //    UserDomainSearch.ShowDialog();
        //}



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
        private async Task DeletUser(object parameter)
        {
            if (SelectedUser == null) return;
            int selectRowID = Convert.ToInt32(SelectedUser.Row["id"]);
            string userName = SelectedUser.Row["UserName"].ToString();

            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить пользователя {SelectedUser.Row["DisplayName"]} ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                SelectedUser = null;
                return;
            }

            try
            {
                bool success = await _userService.DeleteUserAsync(userName);
                if (success)
                {
                    MessageBox.Show(
                $"Пользователь {userName} успешно удален",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

                GetUsers(); //ЕОБХОДИМО ПЕРЕДЕЛАТЬ НА АСИНХРОННЫЙ МЕТОД
                    // Обновляем список пользователей
                    //await LoadUsersAsync();
                }
                else
                {
                    MessageBox.Show(
                        "Не удалось удалить пользователя",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Нельзя удалить собственный аккаунт"))
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                SelectedUser = null;
            }
            
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
            UsersTable = LoadUsers();
        }

        private DataTable LoadUsers()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    var dataTable = new DataTable();

                    connection.Open();
                    var command = new SQLiteCommand("SELECT * FROM Users", connection);

                    using (var adapter = new SQLiteDataAdapter(command))
                    {
                        //var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        //UsersTable = dataTable;
                    }
                    return dataTable;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}");
                return new DataTable();
            }
        }



        public async void UpdateUserAccess(string username, bool isAllowed)
        {
            try
            {
                await _userService.UpdateUserAccessAsync(username, isAllowed);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления доступа: {ex.Message}");
            }
        }


        public async Task ChangeUserRole(string username, int SelRole)
        {
            try
            {
                await _userService.ChangeUserRoleAsync(username, SelRole);
                MessageBox.Show("Роль пользователя изменена");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
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
