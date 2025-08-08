using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace FlowEvents
{
    public class FindUserModel : INotifyPropertyChanged
    {
        private string _connectionString;
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                //GetUnits();
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

        private UserManagerModel _userManagerModel;

        public ObservableCollection<DomainUserModel> Users { get; set; } = new ObservableCollection<DomainUserModel>();

        private CancellationTokenSource _cts;
           
        public RelayCommand SearchCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand AddDomainUserCommand { get; }


        public FindUserModel(MainViewModel mainViewModel, UserManagerModel userManagerModel)
        {
            _mainViewModel = mainViewModel;
            _userManagerModel = userManagerModel;

            DomainName = App.Settings.DomenName; // Имя домена по умолчанию

            ConnectionString = _mainViewModel._connectionString;

            SearchCommand = new RelayCommand(async (param) => await LoadDomainUserAsync());
            CancelCommand = new RelayCommand(CancelSearch);
            AddDomainUserCommand = new RelayCommand(AddDomainUser);
          //  _userManagerModel = userManagerModel;
        }



        private async Task LoadDomainUserAsync()
        {
            _cts = new CancellationTokenSource();
            try
            {
                IsLoading = false;
                Users.Clear();
                var users = await DomainHelper.FindDomainUserAsync(_nameUser, _domainName, _countUsers, _cts.Token);
                foreach (var user in users)
                {
                    Users.Add(user);
                    //MessageBox.Show("DomainName = " + user.DomainName + ", Username = " + user.Username + ", DisplayName = " + user.DisplayName + ", Email = " + user.Email);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show( $"Ошибка: {ex.Message}");
            }
            finally
            {
                IsLoading = true;
            }
        }

        private void CancelSearch(object parameter)
        {
            _cts?.Cancel();
            IsLoading = true;

        }

        private void LoadDomainUser(object parameter)
        {
            try
            {
                var users = DomainHelper.FindDomainUser(_nameUser, _domainName, _countUsers); 
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                   // MessageBox.Show("DomainName = " + user.DomainName + ", Username = " + user.Username + ", DisplayName = " + user.DisplayName + ", Email = " + user.Email);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}");
            }
        }


        private void AddDomainUser(object parameter)
        {
            if (SelectedDomainUser == null) return;

            if (!IsUserUnique(SelectedDomainUser.Username)) // Проверка на наличие данного пользователя в БД
            {
                MessageBox.Show("Пользователь с таким именем уже есть!");
                SelectedDomainUser = null; //Снимаем выделение строки
                return;
            }

            try // Сохранение в базу
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "INSERT INTO Users (UserName, DomainName, DisplayName, Email, RoleId) " +
                        "VALUES (@UserName, @DomainName, @DisplayName, @Email, @RoleId)",
                        connection);

                    command.Parameters.AddWithValue("@UserName", SelectedDomainUser.Username);
                    command.Parameters.AddWithValue("@DomainName", string.IsNullOrEmpty(SelectedDomainUser.DomainName) ? DBNull.Value : (object)SelectedDomainUser.DomainName);
                    command.Parameters.AddWithValue("@DisplayName", string.IsNullOrEmpty(SelectedDomainUser.DisplayName) ? DBNull.Value : (object)SelectedDomainUser.DisplayName);
                    command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(SelectedDomainUser.Email) ? DBNull.Value : (object)SelectedDomainUser.Email);
                    command.Parameters.AddWithValue("@RoleId", 1); // Права пользователя -  0 = user
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException ex) // Обработка ошибок, связанных с SQLite
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) // Обработка всех остальных ошибок
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
           
            MessageBox.Show($"Пользователь {SelectedDomainUser.Username} добавлен", "Уведомление");
            SelectedDomainUser = null; //Снимаем выделение строки

            //необходимо обновлять таблицу с пользователями на странице UserManger
            _userManagerModel?.GetUsers();
        }


        
        // Проверка пользователя на уникальность
        private bool IsUserUnique(string userName)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("SELECT COUNT(*) FROM Users WHERE UserName = @UserName", connection);
                    command.Parameters.AddWithValue("@UserName", userName);
                    return Convert.ToInt32(command.ExecuteScalar()) == 0;
                }
            }
            catch (SQLiteException ex)
            {
                // Обработка ошибок, связанных с SQLite
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        //----------------------------------------
        // свойства для привязки к полям ввода и редактирования данных.
        private string _nameUser ="*";
        public string NameUser
        {
            get => _nameUser;
            set
            {
                if (_nameUser != value)
                {
                    _nameUser = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _domainName; // = "corp.lukoil.com";
        public string DomainName
        {
            get => _domainName;
            set
            {
                _domainName = value;
                App.Settings.DomenName = value;
                OnPropertyChanged();
            }
        }

        private string _countUsers = "5";
        public string CountUsers
        {
            get => _countUsers;
            set
            {
                _countUsers = value;
                OnPropertyChanged();
            }
        }
        
        // Состояние поиска пользователей
        private bool _isLoading =true;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }


        // Объект для размещения данных выделенной строки. При выборе строки таблицы в переменную поместятся все значения выделенной строки
        private DomainUserModel _selectedDomainUser;
        public DomainUserModel SelectedDomainUser
        {
            get => _selectedDomainUser;
            set
            {
                _selectedDomainUser = value;
                OnPropertyChanged();

                // В случае если выбрана какая либо строка , то загружаем данные этой троки в поля для редактирования и отображаем окно редактирования
                if (_selectedDomainUser != null)
                {
                    
                }
                else
                {
                    
                }
            }
        }
    }

    
}
