using FlowEvents.Models;
using FlowEvents.Services;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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


        public PermissionViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;

            Users = new ObservableCollection<User>();


            LoadDataCommand = new RelayCommand(ExecuteLoadDataAsync);
           // LoadDataCommand = new RelayCommand(async (param) => await LoadDataAsync());

            // Автоматическая загрузка данных при старте
            LoadDataCommand.Execute(null);
        }

        private async Task ExecuteLoadDataAsync(object parameter)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                var users = await _databaseService.GetUsersAsync();
                //var roles = await _databaseService.GetRolesAsync();

                Users = new ObservableCollection<User>(users);
                //Roles = new ObservableCollection<Role>(roles);

                // Заглушка для прав (в следующей итерации будем загружать из БД)
                //LoadDemoPermissions();
            }
            finally
            {
                IsLoading = false;
            }
        }





    }
}
