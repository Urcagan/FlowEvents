using FlowEvents.Models;
using System;
using System.Windows;

namespace FlowEvents.Views
{
    /// <summary>
    /// Логика взаимодействия для UserInfoWindow.xaml
    /// </summary>
    public partial class UserInfoWindow : Window
    {
        private UserInfo _userInfo;
        public UserInfoWindow()
        {
            InitializeComponent();

            LoadUserInfo();
            this.DataContext = _userInfo;
        }

        private void LoadUserInfo()
        {
            _userInfo = UserInfoService.GetCurrentUserInfo();

            // Обновляем DataContext если он уже установлен
            if (this.DataContext != null)
            {
                this.DataContext = null;
                this.DataContext = _userInfo;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUserInfo();
            MessageBox.Show("Информация обновлена!", "Обновление",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_userInfo.ToString());
                MessageBox.Show("Все данные скопированы в буфер обмена!", "Копирование",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
