using FlowEvents.Models;
using FlowEvents.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

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

        public PermissionViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;

            Users = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();
            Permissions = new ObservableCollection<Permission>();

            LoadDataCommand = new RelayCommand(async (patam) => await LoadDataAsync());
            SelectAllCommand = new RelayCommand(SelectAllPermissions);
            DeselectAllCommand = new RelayCommand(DeselectAllPermissions);

            // Автоматическая загрузка данных при старте
            LoadDataCommand.Execute(null);
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
    }
}
