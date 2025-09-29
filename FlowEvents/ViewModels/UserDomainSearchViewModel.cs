using FlowEvents.Services;
using FlowEvents.Services.Interface;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.ViewModels
{
    public class UserDomainSearchViewModel : INotifyPropertyChanged
    {
        private readonly IActiveDirectoryService _adService;
        private readonly IDomainSettingsService _domainSettingsService;
        private readonly IUserService _userService;

        private CancellationTokenSource _cancellationTokenSource;

        private string _searchTerm;
        private string _maxResults;
        private bool _onlyActive = true;
        private bool _isSearching;
        private DomainUser _selectedUser;
        private string _domainNameToFaind;
        private bool _isLoading;


        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                if (!string.IsNullOrWhiteSpace(value) && value.Length > 2)
                {
                    StartDelayedSearch();
                }
            }
        }
        public string MaxResults
        {
            get => _maxResults;
            set
            {
                _maxResults = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MaxResultsValidation));
            }
        }

        public string MaxResultsValidation
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MaxResults))
                    return "Введите число";
                if (!int.TryParse(MaxResults, out int result))
                    return "Не число";
                if (result < 1)
                    return "Минимум 1";
                if (result > 1000)
                    return "Максимум 1000";
                return "✓";
            }
        }

        public bool OnlyActive
        {
            get => _onlyActive;
            set
            {
                _onlyActive = value;
                OnPropertyChanged();
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SearchStatus));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public DomainUser SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
            }
        }

        public string DomainNameToFaind
        {
            get => _domainNameToFaind;
            set
            { _domainNameToFaind = value; OnPropertyChanged(); }
        }

        private const int DefaultRoleId = 2; // ID роли "user"

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public ObservableCollection<DomainUser> Users { get; set; }
        public int ResultsCount => Users?.Count ?? 0;
        public RelayCommand SearchCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand CancelSearchCommand { get; }
        public RelayCommand AddDomainUserCommand { get; }
        public RelayCommand WindowClossingCommand { get; }


        public UserDomainSearchViewModel(IActiveDirectoryService adService, IDomainSettingsService domainSettingsService, IUserService userService)
        {
            _adService = adService;
            _domainSettingsService = domainSettingsService;
            _userService = userService;

            Users = new ObservableCollection<DomainUser>();
            DomainNameToFaind = _domainSettingsService.GetCurrentDomainController(); //Задаем домен поиска полученное из файла конфигурации
            _maxResults = "50"; // Значение по умолчанию

            SearchCommand = new RelayCommand(async () => await SearchUsersAsync(),
                () => !string.IsNullOrWhiteSpace(SearchTerm) && !IsSearching && IsValidMaxResults());

            ClearCommand = new RelayCommand(ClearSearch);
            CancelSearchCommand = new RelayCommand(CancelSearch, () => IsSearching);
            AddDomainUserCommand = new RelayCommand(AddDomainUser);
            WindowClossingCommand = new RelayCommand(OnWindowsClosing);
        }





        private async void AddDomainUser() // Добавить доменного пользователя
        {
            if (SelectedUser == null) return;

            try
            {
                IsLoading = true;

                var result = await _userService.RegisterDomainUserAsync(SelectedUser.Username, SelectedUser.DomainName, SelectedUser.DisplayName, SelectedUser.Email, DefaultRoleId);

                if (result.Success)
                {
                    MessageBox.Show(result.Message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Очистка полей
                    SelectedUser = null; //Снимаем выделение строки
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

            //необходимо обновлять таблицу с пользователями на странице UserManger
            ///_userManagerModel?.GetUsers();
        }


        public string SearchStatus
        {
            get
            {
                if (IsSearching)
                    return "Поиск...";
                return $"Найдено пользователей: {ResultsCount}";
            }
        }


        private bool IsValidMaxResults()
        {
            return int.TryParse(MaxResults, out int result) && result >= 1 && result <= 1000;
        }

        private async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm) || !IsValidMaxResults())
                return;

            IsSearching = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                int maxResultsValue = int.Parse(MaxResults);

                var options = new DomainSearchOptions // Настраеваем конфигурацию поиска 
                {
                    SearchTerm = SearchTerm,                                                // искомыое имя пользоваьеля 
                    DomainController = DomainNameToFaind, //Задаем домен поиска
                    MaxResults = maxResultsValue,                                           // количество искомых пользователей
                    OnlyActive = OnlyActive                                                 // состояние активности пользователя
                };

                var result = await _adService.SearchUsersAsync(options, _cancellationTokenSource.Token);

                if (result.IsSuccess)
                {
                    Users.Clear();
                    foreach (var user in result.Users)
                    {
                        Users.Add(user);
                    }
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage, "Ошибка поиска", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                // Поиск отменен - это нормально
            }
            catch (FormatException)
            {
                MessageBox.Show("Некорректное количество результатов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void ClearSearch()
        {
            SearchTerm = string.Empty;
            Users.Clear();
            MaxResults = "50";
            OnlyActive = false;
        }

        private void CancelSearch()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async void StartDelayedSearch()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(800, _cancellationTokenSource.Token);
                if (!_cancellationTokenSource.Token.IsCancellationRequested && IsValidMaxResults())
                {
                    await SearchUsersAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Задача отменена - игнорируем
            }
        }

        private void OnWindowsClosing(object paramerts) // Обработчик закрытия окна 
        {
            if (DomainNameToFaind != _domainSettingsService.GetCurrentDomainController()) 
            _domainSettingsService.SaveDomainSettings(DomainNameToFaind); // App.Settings.SaveSettingsApp(); // Сохранение имя домена в конфигурации приложения в файле cfg

            //if (paramerts is Window window)
            //{
            //    window.Close();
            //}
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        //++++++++++++++++++


        // 🔧 МЕТОД ДЛЯ ЗАГРУЗКИ СВОЙСТВ ВЫБРАННОГО ПОЛЬЗОВАТЕЛЯ
        //public void LoadUserProperties(DomainUser selectedUser)
        //{
        //    if (selectedUser == null)
        //    {
        //        SelectedUserProperties = "Выберите пользователя в таблице для просмотра свойств";
        //        return;
        //    }

        //    try
        //    {
        //        using (var context = new PrincipalContext(ContextType.Domain, _domainSettingsService.GetCurrentDomainController()))
        //        {
        //            // Находим пользователя по логину
        //            var userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, selectedUser.Username);

        //            if (userPrincipal != null)
        //            {
        //                var directoryEntry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;
        //                var propertiesText = new StringBuilder();

        //                propertiesText.AppendLine($"=== СВОЙСТВА ПОЛЬЗОВАТЕЛЯ: {selectedUser.Username} ===");
        //                propertiesText.AppendLine();

        //                // Перебираем все свойства
        //                foreach (string propertyName in directoryEntry.Properties.PropertyNames)
        //                {
        //                    try
        //                    {
        //                        var property = directoryEntry.Properties[propertyName];
        //                        propertiesText.AppendLine($"{propertyName}:");

        //                        foreach (var value in property)
        //                        {
        //                            propertiesText.AppendLine($"  {value}");
        //                        }
        //                        propertiesText.AppendLine();
        //                    }
        //                    catch
        //                    {
        //                        propertiesText.AppendLine($"{propertyName}: [Ошибка чтения]");
        //                        propertiesText.AppendLine();
        //                    }
        //                }

        //                SelectedUserProperties = propertiesText.ToString();
        //            }
        //            else
        //            {
        //                SelectedUserProperties = "Пользователь не найден в Active Directory";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        SelectedUserProperties = $"Ошибка загрузки свойств: {ex.Message}";
        //    }
        //}

    }
}
