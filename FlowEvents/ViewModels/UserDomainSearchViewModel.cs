using FlowEvents.Services.Interface;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        private CancellationTokenSource _cancellationTokenSource;

        public UserDomainSearchViewModel(IActiveDirectoryService adService, IDomainSettingsService domainSettingsService)
        {
            _adService = adService;
            _domainSettingsService = domainSettingsService;

            Users = new ObservableCollection<DomainUser>();
            _maxResults = "50"; // Значение по умолчанию

            SearchCommand = new RelayCommand(async () => await SearchUsersAsync(),
                () => !string.IsNullOrWhiteSpace(SearchTerm) && !IsSearching && IsValidMaxResults());

            ClearCommand = new RelayCommand(ClearSearch);
            CancelSearchCommand = new RelayCommand(CancelSearch, () => IsSearching);
        }

        private string _searchTerm;
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

        private string _maxResults;
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

        private bool _onlyActive = true;
        public bool OnlyActive
        {
            get => _onlyActive;
            set
            {
                _onlyActive = value;
                OnPropertyChanged();
            }
        }

        private bool _isSearching;
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

        public string SearchStatus
        {
            get
            {
                if (IsSearching)
                    return "Поиск...";
                return $"Найдено пользователей: {ResultsCount}";
            }
        }

        public ObservableCollection<DomainUser> Users { get; set; }
        public int ResultsCount => Users?.Count ?? 0;

        public RelayCommand SearchCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand CancelSearchCommand { get; }

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
                    DomainController = _domainSettingsService.GetCurrentDomainController(), //Задаем домен поиска
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
