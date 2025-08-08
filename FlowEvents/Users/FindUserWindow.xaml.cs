using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для FindUserWindow.xaml
    /// </summary>
    public partial class FindUserWindow : Window
    {
        public DomainUserModel SelectedUser { get; private set; }
        public UserManagerModel _userManagerModel { get; }
        public int SelectedRoleId { get; private set; }


            //public FindUserWindow(MainViewModel mainViewModel)
        public FindUserWindow(UserManagerModel userManagerModel)
        {
            InitializeComponent();
            //DataContext = new FindUserModel(mainViewModel);
            _userManagerModel = userManagerModel;
            
            // НЕОБХОДИМО ПЕРЕСМОТРЕТЬ НЕОБХОДИМОСТЬ ПЕРЕДАЧИ ДВУХ ПАРАМЕТРОВ ============================
            DataContext = new FindUserModel(_userManagerModel.MainViewModel, userManagerModel);
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
