using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    public class PaginationInfo : INotifyPropertyChanged
    {
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalItems;
        private int _totalPages;

        public int CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); UpdateCommands(); }
        }

        public int PageSize
        {
            get => _pageSize;
            set { _pageSize = value; OnPropertyChanged(); UpdateCommands(); }
        }

        public int TotalItems
        {
            get => _totalItems;
            set { _totalItems = value; CalculateTotalPages(); }
        }

        public int TotalPages
        {
            get => _totalPages;
            private set { _totalPages = value; OnPropertyChanged(); UpdateCommands(); }
        }

        public ObservableCollection<int> PageSizeOptions { get; } = new ObservableCollection<int> { 20, 50, 100, 200 };

        public RelayCommand FirstPageCommand { get; }
        public RelayCommand PreviousPageCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand LastPageCommand { get; }

        public PaginationInfo()
        {
            FirstPageCommand = new RelayCommand(() => CurrentPage = 1, () => CurrentPage > 1);
            PreviousPageCommand = new RelayCommand(() => CurrentPage--, () => CurrentPage > 1);
            NextPageCommand = new RelayCommand(() => CurrentPage++, () => CurrentPage < TotalPages);
            LastPageCommand = new RelayCommand(() => CurrentPage = TotalPages, () => CurrentPage < TotalPages);
        }

        private void CalculateTotalPages()
        {
            TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
            OnPropertyChanged(nameof(TotalPages));
        }

        private void UpdateCommands()
        {
            FirstPageCommand.RaiseCanExecuteChanged();
            PreviousPageCommand.RaiseCanExecuteChanged();
            NextPageCommand.RaiseCanExecuteChanged();
            LastPageCommand.RaiseCanExecuteChanged();
        }

        public void Reset()
        {
            CurrentPage = 1;
            TotalItems = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
