
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.Users
{
    /// <summary>
    /// Логика взаимодействия для AddUserWindow.xaml
    /// </summary>
    public partial class AddUserWindow : Window
    {
        public UserManagerModel _userManagerModel { get; }
        public AddUserWindow(UserManagerModel userManagerModel)
        {
            InitializeComponent();

            _userManagerModel = userManagerModel;

            //DataContext = new AddUserViewModel(_userManagerModel.MainViewModel, userManagerModel);
            DataContext = new AddUserViewModel( userManagerModel);

            // Привязываем PasswordBox к ViewModel (так как PasswordBox не поддерживает обычную привязку)
            txtPassword.PasswordChanged += (sender, e) =>
            {
                if (DataContext is AddUserViewModel vm)
                {
                    vm.Password = txtPassword.Password;
                }
            };
            txtConfirmPassword.PasswordChanged += (sender, e) =>
            {
                if (DataContext is AddUserViewModel vm)
                {
                    vm.ConfirmPassword = txtConfirmPassword.Password;
                }
            };
        }

        public AddUserWindow()
        {
        }

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
