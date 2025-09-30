using FlowEvents.ViewModels;
using FlowEvents.Views;
using FlowEvents.Models;
using FlowEvents.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using FlowEvents.Services.Interface;

namespace FlowEvents.Users
{
    public class PermissionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly IDatabaseService _databaseService;
        private readonly IUserService _userService;    

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

        public PermissionViewModel(IDatabaseService databaseService, IUserService userService)
        {
            _databaseService = databaseService;
            _userService = userService;

            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();
            Permissions = new ObservableCollection<Permission>();

            LoadDataCommand = new RelayCommand(async (patam) => await LoadDataAsync());
            SelectAllCommand = new RelayCommand(SelectAllPermissions);
            DeselectAllCommand = new RelayCommand(DeselectAllPermissions);
            OpenAddUserWindowCommand = new RelayCommand(OpenAddUserWindows);
            DomainUserSearchWindowCommand = new RelayCommand(DomainUserSearchWindow);
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync());

            // Автоматическая загрузка данных при старте
            LoadDataCommand.Execute(null);
        }


        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Выберите пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (await _userService.DeleteUserAsync(SelectedUser.UserName))
            {
                MessageBox.Show("Пользователь удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Имитация задержки сети (2 секунды)
                await Task.Delay(1000);

                var users = await _databaseService.GetUsersAsync();

                var roles = await _databaseService.GetRolesAsync();

                var permissions = await _databaseService.GetPermissionsAsync();

                Users = new ObservableCollection<User>(users);
                Roles = new ObservableCollection<Role>(roles);
                Permissions = new ObservableCollection<Permission>(permissions);

            }
            finally
            {
                IsLoading = false;
            }
        }


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
               // GetUsers();  // Перезагружаем установки после закрытия окна UnitsView
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
                                               // GetUsers();  // Перезагружаем установки после закрытия окна UnitsView
            }
            UserDomainSearch.Closed += ClosedHandler; // Подписываемся на событие закрытия окна
            UserDomainSearch.ShowDialog();
        }
    }
}
