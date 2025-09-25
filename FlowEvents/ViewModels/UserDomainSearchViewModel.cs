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

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly IActiveDirectoryService _adService;
        private readonly IDomainSettingsService _domainSettingsService;

        private string _searchTerm;
        private bool _isSearching;
        private ObservableCollection<DomainUser> _users;
        private CancellationTokenSource _cancellationTokenSource;

        public UserDomainSearchViewModel(IActiveDirectoryService adService, IDomainSettingsService domainSettingsService)
        {
            _adService = adService;
            _domainSettingsService = domainSettingsService;

            Users = new ObservableCollection<DomainUser>();

            SearchCommand = new RelayCommand(async () => await SearchUsersAsync(),
                () => !string.IsNullOrWhiteSpace(SearchTerm) && !IsSearching);
            
            ClearCommand = new RelayCommand(ClearSearch);
            
            CancelSearchCommand = new RelayCommand(CancelSearch);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                _searchTerm = value;
                OnPropertyChanged();
                // Автопоиск при вводе (с задержкой)
                if (!string.IsNullOrWhiteSpace(value) && value.Length > 2) // Запускать поиск при длинне искомого имени более заданного значения
                {
                    StartDelayedSearch();
                }
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<DomainUser> Users
        {
            get => _users;
            set
            {
                _users = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResultsCount));
            }
        }

        public int ResultsCount => Users?.Count ?? 0;

        public RelayCommand SearchCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand CancelSearchCommand { get; }

        private async Task SearchUsersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm)) return;

            IsSearching = true;
            _cancellationTokenSource = new CancellationTokenSource(); //это механизм для отмены асинхронных операций в C#.

            try
            {
                var options = new DomainSearchOptions
                {
                    SearchTerm = SearchTerm,
                    DomainController = _domainSettingsService.GetCurrentDomainController(),
                    MaxResults = 50
                };

                var result = await _adService.SearchUsersAsync(options, _cancellationTokenSource.Token);

                if (result.IsSuccess)
                {
                    Users = new ObservableCollection<DomainUser>(result.Users);
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage, "Ошибка поиска");
                }
            }
            catch (OperationCanceledException)
            {
                // Поиск отменен - это нормально
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
        }

        private void CancelSearch()             // Когда пользователь нажимает кнопку "Остановить поиск"
        {
            _cancellationTokenSource?.Cancel(); // ← ОТМЕНА!
        }

        // Автопоиск с задержкой
        private async void StartDelayedSearch()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(500, _cancellationTokenSource.Token);
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await SearchUsersAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Задача отменена - игнорируем
            }
        }
    }
}
