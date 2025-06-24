using System;
using System.Collections.Generic;
using System.Data;
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

namespace FlowEvents.Properties
{
    /// <summary>
    /// Логика взаимодействия для FindUserWindow.xaml
    /// </summary>
    public partial class FindUserWindow : Window
    {
        public DomainUser SelectedUser { get; private set; }
        public int SelectedRoleId { get; private set; }

        private DataTable _rolesTable;

        public FindUserWindow(DataTable rolesTable)
        {
            InitializeComponent();

         //   _rolesTable = rolesTable;
         //   RoleComboBox.ItemsSource = _rolesTable.DefaultView;
          //  RoleComboBox.DisplayMemberPath = "RoleName";
          //  RoleComboBox.SelectedValuePath = "RoleId";
        }
        /*
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersListBox.SelectedItem is DomainUser user &&
                RoleComboBox.SelectedValue is int roleId)
            {
                SelectedUser = user;
                SelectedRoleId = roleId;
                DialogResult = true;
                Close();
            }
        }
        */
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
