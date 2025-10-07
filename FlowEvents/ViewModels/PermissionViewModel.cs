using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services;
using FlowEvents.Services.Interface;
using FlowEvents.ViewModels;
using FlowEvents.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents.Users
{
    public class PermissionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly IUserService _userService;   
        private readonly IRoleServices _roleServices;

        private readonly IPermissionService _permissionService;

        private ObservableCollection<User> _users;
        private ObservableCollection<Role> _roles;
        private ObservableCollection<Permission> _permissions;
        private User _selectedUser;
        private Role _selectedRole;
        private bool _isLoading;

        public ObservableCollection<User> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged(nameof(Users));
            }
        }
        public ObservableCollection<Role> Roles
        {
            get => _roles;
            set
            {
                _roles = value;
                OnPropertyChanged(nameof(Roles));
            }
        }
        public ObservableCollection<Permission> Permissions
        {
            get => _permissions;
            set
            {
                _permissions = value;
                OnPropertyChanged(nameof(Permissions));
            }
        }
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                if (value != null)
                {
                    // Загрузка прав для выбранного пользователя
                    LoadUserPermissions();
                    OnPropertyChanged(nameof(SelectedUser));
                }
            }
        }
        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                _selectedRole = value;
                if (value != null)
                {
                    // Загрузка прав для выбранной роли
                    LoadRolePermissions();
                    OnPropertyChanged(nameof(SelectedRole));
                }
            }
        }
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public RelayCommand LoadDataCommand { get; }
        public RelayCommand SelectAllCommand { get; }
        public RelayCommand DeselectAllCommand { get; }
        public RelayCommand OpenAddUserWindowCommand { get; }
        public RelayCommand DomainUserSearchWindowCommand { get; }
        public RelayCommand DeleteUserCommand { get; }
        public RelayCommand<User> UpdateUserAccessCommand { get; }
        public RelayCommand<User> UpdateUserRoleCommand { get; }    // Команда обновления роли пользователя

        public PermissionViewModel( IUserService userService, IRoleServices roleServices, IPermissionService permissionService)
        {
            
            _userService = userService;
            _roleServices = roleServices;

            _permissionService = permissionService;

            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();
            Permissions = new ObservableCollection<Permission>();

            LoadDataCommand = new RelayCommand(async (patam) => await LoadDataAsync());
            SelectAllCommand = new RelayCommand(SelectAllPermissions);
            DeselectAllCommand = new RelayCommand(DeselectAllPermissions);
            OpenAddUserWindowCommand = new RelayCommand(OpenAddUserWindows);
            DomainUserSearchWindowCommand = new RelayCommand(DomainUserSearchWindow);
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => SelectedUser != null);

            UpdateUserAccessCommand = new RelayCommand<User>(async (user) => await UpdateUserAccessAsync(user)); // Изменения доступа пользователя

            UpdateUserRoleCommand = new RelayCommand<User>(async (user) => await UpdateUserRoleAsync(user));

            // Автоматическая загрузка данных при старте
            LoadDataCommand.Execute(null);
            _roleServices = roleServices;
        }


        // Метод для обновления роли
        private async Task UpdateUserRoleAsync(User user)
        {
            // Блокируем выполнение команды только во время загрузки данных 
            if (_isLoading || user == null) return;

            // Проверяем, что пользователь существует
            if (user == null) return;

            try
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите изменить роль пользователя '{user.UserName}'?",
                    "Подтверждение изменения роли",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                // ВЫПОЛНЯЕМ ОБНОВЛЕНИЕ В БАЗЕ ДАННЫХ
                // Передаем логин пользователя и новый RoleId
                await _userService.ChangeUserRoleAsync(user.UserName, user.RoleId);

                // ОБНОВЛЯЕМ ОБЪЕКТ ROLE У ПОЛЬЗОВАТЕЛЯ - ключевой момент!
                // Ищем новую роль в коллекции Roles по обновленному RoleId
                var newRole = Roles.FirstOrDefault(r => r.RoleId == user.RoleId);

                // Устанавливаем найденную роль пользователю
                user.Role = newRole;

                //MessageBox.Show("Роль пользователя успешно изменена", "Успех",
                //               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // ОБРАБОТКА ОШИБОК - показываем сообщение пользователю
                MessageBox.Show($"Ошибка изменения роли: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private async Task UpdateUserRoleAsync(User user)
        //{
        //    if (user == null) return;

        //    try
        //    {
        //        // Подтверждение изменения роли
        //        var result = MessageBox.Show(
        //            $"Вы уверены, что хотите изменить роль пользователя '{user.UserName}'?",
        //            "Подтверждение изменения роли",
        //            MessageBoxButton.YesNo,
        //            MessageBoxImage.Question);

        //        if (result != MessageBoxResult.Yes) return;

        //        await _userService.ChangeUserRoleAsync(user.UserName, user.RoleId);

        //        // Обновляем отображаемое имя роли
        //        //user.OnPropertyChanged(nameof(User.RoleName));
        //        user.RefreshRoleName(); 

        //        //MessageBox.Show("Роль пользователя успешно изменена", "Успех",
        //        //               MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка изменения роли: {ex.Message}", "Ошибка",
        //                       MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}


        // Обновление доступа пользователя
        private async Task UpdateUserAccessAsync(User user)
        {
            if (user == null) return;

            try
            {
                // Показываем подтверждение для отключения доступа
                if (!user.IsAllowedBool)
                {
                    var result = MessageBox.Show(
                        $"Вы уверены, что хотите отключить доступ пользователю '{user.UserName}'?",
                        "Подтверждение отключения доступа",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        // Отменяем изменение, если пользователь сказал "Нет"
                        user.IsAllowedBool = true;
                        return;
                    }
                }

                // Обновляем доступ через сервис
                await _userService.UpdateUserAccessAsync(user.UserName, user.IsAllowedBool);

                //MessageBox.Show("Доступ пользователя успешно обновлен", "Успех",
                //               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Откатываем изменение в случае ошибки
                user.IsAllowedBool = !user.IsAllowedBool;
                MessageBox.Show($"Ошибка обновления доступа: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        // Удаление пользователя
        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления", "Информация",
                       MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var result = MessageBox.Show($"Вы уверены, что хотите удалить пользователя? ' {SelectedUser.UserName}' ?",
                                        "Подтверждение удаления",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning,
                                        MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _userService.DeleteUserAsync(SelectedUser.UserName);
                    Users.Remove(SelectedUser);
                    MessageBox.Show("Пользователь успешно удален", "Успех",
                           MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }                
            }
        }

        private async Task LoadDataAsync()
        {
            // Устанавливаем флаг загрузки - это обновляет UI (показывает индикатор загрузки)
            IsLoading = true;

            try
            {
                // Параллельно загружаем все необходимые данные для производительности
                var usersTask = _userService.GetUsersAsync();                           // Загрузка пользователей из БД
                var rolesTask = _roleServices.GetRolesAsync();                          // Загрузка ролей из БД  
                var permissionsTask = _permissionService.GetPermissionsAsync();         // Загрузка прав из БД

                // Ожидаем завершения всех трех задач одновременно
                await Task.WhenAll(usersTask, rolesTask, permissionsTask);

                // Получаем результаты задач
                var users = await usersTask;         // List<User> - все пользователи
                var roles = await rolesTask;         // List<Role> - все роли
                var permissions = await permissionsTask; // List<Permission> - все права

                // СОЗДАЕМ СЛОВАРЬ РОЛЕЙ - это ключевой момент для производительности
                // Преобразуем список ролей в словарь, где ключ - RoleId, значение - объект Role
                // Это позволяет мгновенно находить роль по ID без перебора списка
                var rolesDictionary = roles.ToDictionary(
                    r => r.RoleId,    // Ключ словаря - идентификатор роли
                    r => r            // Значение - сам объект роли
                );

                // ПРИВЯЗЫВАЕМ РОЛИ К ПОЛЬЗОВАТЕЛЯМ - основная логика
                // Проходим по всем пользователям и находим их роли по RoleId
                foreach (var user in users)
                {
                    // Пытаемся найти роль пользователя в словаре по его RoleId
                    if (rolesDictionary.TryGetValue(user.RoleId, out var role))
                    {
                        // Если роль найдена - устанавливаем свойство Role у пользователя
                        user.Role = role;

                        // ЗАЧЕМ ЭТО НУЖНО:
                        // 1. Теперь у каждого пользователя есть полный объект его роли
                        // 2. В XAML можно делать привязки типа {Binding Role.RoleName}
                        // 3. RoleName будет автоматически обновляться при изменении Role
                    }
                    else
                    {
                        // Если роль не найдена (например, удалена из БД) - обрабатываем эту ситуацию
                        // Можно установить null или создать заглушку
                        user.Role = null;
                    }
                }

                // ОБНОВЛЯЕМ КОЛЛЕКЦИИ ДАННЫХ - уведомляем UI об изменениях
                Users = new ObservableCollection<User>(users);          // Коллекция пользователей
                Roles = new ObservableCollection<Role>(roles);          // Коллекция ролей (для ComboBox)
                Permissions = new ObservableCollection<Permission>(permissions); // Коллекция прав
            }
            finally
            {
                // Сбрасываем флаг загрузки в любом случае - даже если была ошибка
                IsLoading = false;
            }
        }

        //private async Task LoadDataAsync()
        //{
        //    // Устанавливаем флаг загрузки - это обновляет UI (показывает индикатор загрузки)
        //    IsLoading = true;

        //    try
        //    {
        //        // Параллельно загружаем все необходимые данные
        //        var users = await _databaseService.GetUsersAsync(); // Загрузка пользователей из БД

        //        var roles = await _databaseService.GetRolesAsync(); // Загрузка ролей из БД  

        //        var permissions = await _databaseService.GetPermissionsAsync(); // Загрузка прав из БД

        //        Users = new ObservableCollection<User>(users);
        //        Roles = new ObservableCollection<Role>(roles);
        //        Permissions = new ObservableCollection<Permission>(permissions);

        //    }
        //    finally
        //    {
        //        IsLoading = false;
        //    }
        //}


        // Загрузка прав для выбранного пользователя
        private void LoadUserPermissions()
        {
            // Заглушка - в следующей итерации реализуем загрузку прав пользователя
            foreach (var permission in Permissions)
            {
                permission.IsGrantedBool = false;
            }
        }

        // Загрузка прав к выбранной роли
        private void LoadRolePermissions()
        {
            if (SelectedUser == null) return;
            int roleUser = SelectedUser.RoleId;
            // Заглушка - в следующей итерации реализуем загрузку прав роли
            foreach (var permission in Permissions)
            {
                //  permission.IsGrantedBool = (SelectedRole?.RoleId % 2 == 0); // Простая логика для демонстрации

                permission.IsGrantedBool = (SelectedRole?.RoleId == SelectedUser.RoleId);
            }
        }

        private void SelectAllPermissions(object parameters)
        {
            foreach (var permission in Permissions)
            {
                permission.IsGrantedBool = true;
            }
        }

        private void DeselectAllPermissions(object parameters)
        {
            foreach (var permission in Permissions)
            {
                permission.IsGrantedBool = false;
            }
        }

        // Окно добавления локального пользователя
        private void OpenAddUserWindows()
        {
            var addUserViewModel = App.ServiceProvider.GetRequiredService<AddUserViewModel>();
            var addUserWindow = new AddUserWindow();
            addUserWindow.DataContext = addUserViewModel;
            addUserWindow.Owner = Application.Current.MainWindow;

            void ClosedHandler(object sender, EventArgs e)
            {
                addUserWindow.Closed -= ClosedHandler; // Отвязываем                                                       
                LoadDataCommand.Execute(null); // переезагрузка данных при закрытии окна добавлннтя пользовател
            }
            addUserWindow.Closed += ClosedHandler; // Подписываемся на событие закрытия окна
            if (addUserWindow.ShowDialog() == true) { }
        }

        private void DomainUserSearchWindow()
        {
            var UserDomainSearchViewModel = App.ServiceProvider.GetRequiredService<UserDomainSearchViewModel>();

            var UserDomainSearch = new UserDomainSearch();
            UserDomainSearch.DataContext = UserDomainSearchViewModel;
            UserDomainSearch.Owner = Application.Current.MainWindow;
            void ClosedHandler(object sender, EventArgs e)
            {
                UserDomainSearch.Closed -= ClosedHandler; // Отвязываем
                LoadDataCommand.Execute(null); // переезагрузка данных при закрытии окна добавлннтя пользовател
            }
            UserDomainSearch.Closed += ClosedHandler; // Подписываемся на событие закрытия окна
            UserDomainSearch.ShowDialog();
        }
    }
}
