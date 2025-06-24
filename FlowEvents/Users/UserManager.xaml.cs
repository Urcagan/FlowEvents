using FlowEvents.Properties;
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

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для UserManager.xaml
    /// </summary>
    public partial class UserManager : Window
    {

        private readonly UserManagerModel _model;

        public UserManager(UserManagerModel userManagerModel)
        {
            InitializeComponent();
            _model = userManagerModel;
            DataContext = _model;

            // Инициализация списка ролей
            var roles = new List<string> { "user", "admin", "manager" };
            Resources.Add("RolesList", roles);
        }

        private void BtnFindInDomain_Click(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindUserWindow(_model.RolesTable);
            findWindow.Owner = this;
            findWindow.ShowDialog();

            // Обновляем список после добавления пользователя
           // LoadUsers();
        }


        //Обработка событий при работе со строкой в таблице пользователей 
        private void DgUsers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var rowView = e.Row.Item as DataRowView;
                if (rowView != null)
                {
                    string username = rowView["Username"].ToString();

                    if (e.Column.Header.ToString() == "Доступ")
                    {
                        bool isAllowed = (bool)((CheckBox)e.EditingElement).IsChecked;
                        _model.UpdateUserAccess(username, isAllowed);
                    }
                    else if (e.Column.Header.ToString() == "Роль")
                    {
                        int newRole = Convert.ToInt32(((ComboBox)e.EditingElement).SelectedValue);
                        _model.UpdateUserRole(username, newRole);
                    }
                }
            }
        }
    }
}
