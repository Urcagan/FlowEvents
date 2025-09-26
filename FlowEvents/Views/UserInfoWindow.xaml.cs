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

        }

        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
