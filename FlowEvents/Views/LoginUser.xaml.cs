using FlowEvents.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

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
            var addUserViewModel = App.ServiceProvider.GetRequiredService<AddUserViewModel>();
            var addUserWindow = new AddUserWindow();
            addUserWindow.DataContext = addUserViewModel;
            addUserWindow.Owner = Application.Current.MainWindow;

            if (addUserWindow.ShowDialog() == true) { }

            //UserManagerModel userManagerModel = new UserManagerModel();
            //var registerWindow = new AddUserWindow();
            //registerWindow.Show();
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close(); // Закрытие текущего окна
        }

    }
}
