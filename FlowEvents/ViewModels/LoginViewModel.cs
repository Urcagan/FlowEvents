using FlowEvents.Models;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username;

        public User CurrentUser { get; private set; }
        //public Action<bool> CloseAction { get; set; }
        public Action CloseAction { get; set; }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        private readonly IConnectionStringProvider _connectionStringProvider;

        public ICommand LoginCommand { get; }

        public LoginViewModel(IConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
            LoginCommand = new RelayCommand(ExecuteLoginCommand, CanExecuteLoginCommand);
        }

        private void ExecuteLoginCommand(object parameter)
        {
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            if (passwordBox == null) return;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var _connectionString = _connectionStringProvider.GetConnectionString();
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"SELECT UserName, Password, Salt, IsAllowed, RoleId, DisplayName, Email, IsLocal 
                                    FROM Users 
                                    WHERE UserName = @username";

                    var command = new SQLiteCommand(query, connection);
                    command.Parameters.AddWithValue("@username", Username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var storedPassword = reader["Password"].ToString();
                            var salt = reader["Salt"].ToString();
                            var isAllowed = Convert.ToInt32(reader["IsAllowed"]) == 1;
                            var roleId = Convert.ToInt32(reader["RoleId"]);

                            if (!isAllowed)
                            {
                                MessageBox.Show("Учетная запись заблокирована", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            var hashedInputPassword = HashPassword(passwordBox.Password, salt);

                            if (hashedInputPassword == storedPassword)
                            {
                                CurrentUser = new User
                                {
                                    UserName = reader["UserName"].ToString(),
                                    RoleId = roleId,
                                    IsAuthenticated = true,
                                    DisplayName = reader["DisplayName"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    IsLocal = Convert.ToInt32(reader["IsLocal"])
                                };

                                MessageBox.Show($"Добро пожаловать, {Username}!", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                                // Закрываем окно с успешным результатом
                                CloseAction?.Invoke();
                            }
                            else
                            {
                                MessageBox.Show("Неверный пароль", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не найден", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteLoginCommand(object parameter)
        {
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            return !string.IsNullOrWhiteSpace(Username) &&
                   passwordBox != null &&
                   !string.IsNullOrWhiteSpace(passwordBox.Password);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private void CloseWindow(Window window, bool dialogResult)
        {
            if (window != null)
            {
                window.DialogResult = dialogResult;
                window.Close();
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
