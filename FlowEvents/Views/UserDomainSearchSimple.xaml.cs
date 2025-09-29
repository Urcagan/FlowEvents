using FlowEvents.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FlowEvents.Views
{
    /// <summary>
    /// Логика взаимодействия для UserDomainSearchSimple.xaml
    /// </summary>
    public partial class UserDomainSearchSimple : Window
    {
        public UserDomainSearchSimple()
        {
            InitializeComponent();
        }

        //private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (DataContext is UserDomainSearchViewModel viewModel)
        //    {
        //        if (sender is DataGrid dataGrid && dataGrid.SelectedItem is DomainUser selectedUser)
        //        {
        //            // Загружаем свойства выбранного пользователя
        //          //  viewModel.LoadUserProperties(selectedUser);
        //        }
        //        else
        //        {
        //            viewModel.SelectedUserProperties = "Выберите пользователя в таблице для просмотра свойств";
        //        }
        //    }
        //}
    }
}
