using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Логика взаимодействия для CategoryWindow.xaml
    /// </summary>
    public partial class CategoryWindow : Window
    {
        private MainWindow _mainWindow;

        private BindingList<CategoryModel> _categoryData;

        public CategoryWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;   
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _categoryData = new BindingList<CategoryModel>()
            {
                new CategoryModel(){Name = "ТО", Description = "Описание" }
            };

            dgCategoryList.ItemsSource = _categoryData;

            _categoryData.ListChanged += _categoryData_ListChanged; // Привязывает событие обновления списка
        }

        private void _categoryData_ListChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    break;
                case ListChangedType.ItemAdded:
                    break;
                case ListChangedType.ItemDeleted:
                    break;
                case ListChangedType.ItemMoved:
                    break;
                case ListChangedType.ItemChanged:
                    break;
                case ListChangedType.PropertyDescriptorAdded:
                    break;
                case ListChangedType.PropertyDescriptorDeleted:
                    break;
                case ListChangedType.PropertyDescriptorChanged:
                    break;
                default:
                    break;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
