using FlowEvents.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FlowEvents.Users
{
    /// <summary>
    /// Логика взаимодействия для LoginUser.xaml
    /// </summary>
    public partial class LoginUser : Window
    {
        public LoginUser()
        {
            InitializeComponent();
            
            LoginViewModel loginViewModel = new LoginViewModel();

            loginViewModel.CloseAction = (result) =>
            {
                DialogResult = result;

                Close();
            };
            DataContext = loginViewModel;
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void RegisterText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UserManagerModel userManagerModel = new UserManagerModel();
            var registerWindow = new AddUserWindow(userManagerModel);
            registerWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close(); // Закрытие текущего окна
        }

    }
}
