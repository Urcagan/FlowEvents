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
using System.Windows.Controls;
using System.Windows.Input;

namespace FlowEvents.Users
{
    public class AddUserViewModel : INotifyPropertyChanged
    {
        // Реализация INotifyPropertyChanged
        // Событие, которое уведомляет об изменении свойства
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private MainViewModel _mainViewModel;
        private string _connectionString;
        private const int DefaultRoleId = 1; // ID роли "user"

        private string _username;
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
        
        private string _confirmPassword;
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


        public AddUserViewModel(MainViewModel mainViewModel, UserManagerModel userManagerModel)
        {
            _mainViewModel = mainViewModel;
            _connectionString = _mainViewModel._connectionString;

            // Инициализация команд

            AddUserCommand = new RelayCommand(AddUser);
            CancelCommand = new RelayCommand(Cancel);
        }

        
        private void AddUser(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                // Проверка на пустые поля
                System.Windows.MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            if (Password != ConfirmPassword)
            {
                // Проверка на совпадение паролей
                System.Windows.MessageBox.Show("Пароли не совпадают.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            var salt = GenerateSalt(); // Генерация соли
            var hashedPassword = HashPassword(Password, salt); // Хеширование пароля
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO Users 
                                (UserName, DomainName, DisplayName, Email, RoleId, IsAllowed, Password, Salt) 
                                VALUES 
                                (@username, '', @username, '', @roleId, 1, @password, @salt)";
                    var command = new SQLiteCommand(query, connection);
                    command.Parameters.AddWithValue("@username", Username);
                    command.Parameters.AddWithValue("@roleId", DefaultRoleId);
                    command.Parameters.AddWithValue("@password", hashedPassword);
                    command.Parameters.AddWithValue("@salt", salt);
                    command.ExecuteNonQuery();
                }
                MessageBox.Show($"Пользователь {Username} добавлен", "Уведомление");
                Username = string.Empty; // Очистка полей после добавления
                Password = string.Empty;
                ConfirmPassword = string.Empty;

                // При успешном добавлении пользователя закрываем окно
                CloseWindow(parameter as Window, false); // 
            }
            catch (SQLiteException ex)
            {
                // Обработка ошибок базы данных
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                window.DialogResult = dialogResult;
                window.Close();
            }
        }

        // Генерация соли для хеширования пароля
        private string GenerateSalt() 
        {
            byte[] saltBytes = new byte[16]; // Размер соли 16 байт
            using (var rng = RandomNumberGenerator.Create()) // Использование криптографически безопасного генератора случайных чисел
            {
                rng.GetBytes(saltBytes); // Заполнение массива случайными байтами
            }
            return Convert.ToBase64String(saltBytes); // Преобразование массива байтов в строку Base64
        }

        // Хеширование пароля с использованием соли
        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create()) // Использование SHA-256 для хеширования
            {
                var saltedPassword = password + salt; // Добавление соли к паролю
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword)); // Хеширование пароля с солью
                return Convert.ToBase64String(hashedBytes); // Возвращение хешированного пароля в виде строки
            }
        }

    }
}
