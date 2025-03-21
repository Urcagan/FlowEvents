using FlowEvents.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static FlowEvents.Global_Var;
using Microsoft.Extensions.DependencyInjection;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private AppSettings appSettings;

        public DatabaseHelper databaseHelper;
        private bool IsCheckDB = false;

        // Конструктор по умолчанию (без параметров)
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            // Подписываемся на событие загрузки окна
            this.Loaded += MainWindow_Loaded;
        }



        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Получаем ViewModel из DataContext
            if (DataContext is MainViewModel viewModel)
            {
                // Вызываем метод загрузки и проверки данных
                viewModel.StartUP();
            }
        }


        private BindingList<EventsModel> _eventsData;



        private void Unit_Click(object sender, RoutedEventArgs e)
        {
            UnitsView unitsView = new UnitsView();
            if(unitsView.ShowDialog() == true)
            {

            }
        }



        private void Category_Click(object sender, RoutedEventArgs e)
        {
            CategoryView categoryView = new CategoryView();
            if (categoryView.ShowDialog() == true)
            {

            }
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            EventView eventView = new EventView();
            if (eventView.ShowDialog() == true) { }
        }
    }


}
