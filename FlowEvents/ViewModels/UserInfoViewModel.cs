using FlowEvents.Models;
using FlowEvents.Services.Interface;
using FlowEvents.Views;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.ViewModels
{
    public class UserInfoViewModel : INotifyPropertyChanged
    {
        private readonly IUserInfoService _userInfoService;
        private UserInfo _userInfo;

        public UserInfoViewModel(IUserInfoService userInfoService)
        {
            _userInfoService = userInfoService;
            LoadUserInfo();
        }


        public UserInfo UserInfo
        {
            get => _userInfo;
            private set
            {
                _userInfo = value;
                OnPropertyChanged(nameof(UserInfo));
                // Уведомляем об изменении всех свойств для привязки
                OnPropertyChanged(nameof(Login));
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(FullLogin));
                OnPropertyChanged(nameof(Domain));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(UserType));
                OnPropertyChanged(nameof(SID));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(DistinguishedName));
            }
        }


        // Свойства для прямого доступа (удобно для привязки в XAML)
        public string Login => UserInfo?.Login ?? string.Empty;
        public string DisplayName => UserInfo?.DisplayName ?? string.Empty;
        public string FullLogin => UserInfo?.FullLogin ?? string.Empty;
        public string Domain => UserInfo?.Domain ?? string.Empty;
        public string Email => UserInfo?.Email ?? string.Empty;
        public string UserType => UserInfo?.UserType ?? string.Empty;
        public string SID => UserInfo?.SID ?? string.Empty;
        public string Description => UserInfo?.Description ?? string.Empty;
        public string DistinguishedName => UserInfo?.DistinguishedName ?? string.Empty;

        // Команды
        public ICommand RefreshCommand => new RelayCommand(_ => RefreshUserInfo());
        public ICommand CloseCommand => new RelayCommand(_ => CloseWindow());
        public ICommand CopyAllCommand => new RelayCommand(_ => CopyAllToClipboard());

        private void LoadUserInfo()
        {
            UserInfo = _userInfoService.GetCurrentUserInfo();
        }

        private void RefreshUserInfo()
        {
            LoadUserInfo();
            MessageBox.Show("Информация обновлена!", "Обновление",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseWindow()
        {
            // Закрытие окна через привязку к DialogResult
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = Application.Current.Windows
                    .OfType<UserInfoWindow>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            });
        }

        private void CopyAllToClipboard()
        {
            try
            {
                var text = $"Логин: {Login}\n" +
                          $"Отображаемое имя: {DisplayName}\n" +
                          $"Полный логин: {FullLogin}\n" +
                          $"Домен: {Domain}\n" +
                          $"Email: {Email}\n" +
                          $"Тип: {UserType}\n" +
                          $"SID: {SID}\n" +
                          $"Описание: {Description}\n" +
                          $"Distinguished Name: {DistinguishedName}";

                Clipboard.SetText(text);
                MessageBox.Show("Все данные скопированы в буфер обмена!", "Копирование",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
