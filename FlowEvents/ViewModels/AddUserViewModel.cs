using FlowEvents.Services;
using FlowEvents.Services.Interface;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.Users
{
    public class AddUserViewModel : INotifyPropertyChanged
    {
        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly IDatabaseService _databaseService;
        private readonly IUserService _userService;

        private bool _isLoading;
        private const int DefaultRoleId =  2; // ID роли "user"

        private string _username;
        private string _password;
        private string _confirmPassword;


        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged(nameof(ConfirmPassword));
            }
        }

        // Команды для добавления пользователя и отмены
        public ICommand AddUserCommand { get; }
        public ICommand CancelCommand { get; }


        public AddUserViewModel(IDatabaseService databaseService, IUserService userService)
        {
            _databaseService = databaseService;
            _userService = userService;

            AddUserCommand = new RelayCommand(AddLocalUser);
            CancelCommand = new RelayCommand(Cancel);
        }


        // Добавить локального пользователя
        private async void AddLocalUser(object parameter)
        {
            try
            {
                IsLoading = true;

                var result = await _userService.RegisterUserAsync(Username, Password, ConfirmPassword, DefaultRoleId); 

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очистка полей
                    Username = string.Empty;
                    Password = string.Empty;
                    ConfirmPassword = string.Empty;

                    // Закрытие окна
                    if (parameter is Window window)
                        window.Close();
                }
                else
                {
                    MessageBox.Show(result.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        

        // Отмена добавления пользователя
        private void Cancel(object parameter)
        {
            // Очистка полей и закрытие окна
            Username = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            CloseWindow(parameter as Window, false);
            //_mainViewModel.CloseAddUserView();
        }

        private void CloseWindow(Window window, bool dialogResult)
        {
            if (window != null)
            {
                //window.DialogResult = dialogResult;
                window.Close();
            }
        }
               
    }
}
