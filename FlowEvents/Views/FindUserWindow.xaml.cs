using System.Windows;
using System.Windows.Controls;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для FindUserWindow.xaml
    /// </summary>
    public partial class FindUserWindow : Window
    {
        public DomainUser SelectedUser { get; private set; }
        public UserManagerModel _userManagerModel { get; }
        public int SelectedRoleId { get; private set; }


            //public FindUserWindow(MainViewModel mainViewModel)
        public FindUserWindow(UserManagerModel userManagerModel)
        {
            InitializeComponent();
            //DataContext = new FindUserModel(mainViewModel);
            //_userManagerModel = userManagerModel;
            
            // НЕОБХОДИМО ПЕРЕСМОТРЕТЬ НЕОБХОДИМОСТЬ ПЕРЕДАЧИ ДВУХ ПАРАМЕТРОВ ============================
            //DataContext = new FindUserModel(_userManagerModel.MainViewModel, userManagerModel);
            DataContext = new FindUserModel( userManagerModel);
        }
        
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
        private void dgResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
