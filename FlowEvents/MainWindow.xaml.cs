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
        }

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем настройки при запуске программы
            appSettings = AppSettings.Load();

            //Необходимо проверить есть ли файл базы данных по указанному пути

            // Проверяем, существует ли база данных
            if (!CheckDB.CheckDatabaseFile(appSettings))
            {
                Application.Current.Shutdown(); // Закрываем приложение, если файл не выбран
            }
            else
            {
                Global_Var.pathDB = appSettings.pathDB;

                databaseHelper = new DatabaseHelper(Global_Var.pathDB);    // Инициализация копии класса работы с БД

                lblPath.Text = "Путь: " + Global_Var.pathDB; //Global_Var.pathDB;

                bool stateDB;   // переменная состояния проверки базы данных
                                // Проверка базы данных на исправность
                if (CheckDB.ALLCheckDB(databaseHelper, Global_Var.pathDB, "Config", appSettings.VerDB))
                {
                    // Создаем и показываем окно настроек
                    //SettingsWindow settingsWindow = new SettingsWindow( databaseHelper );
                    //settingsWindow.ShowDialog(); // Открываем окно как модальное
                    IsCheckDB = true;
                }
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

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Создаем и показываем окно настроек
            SettingsWindow settingsWindow = new SettingsWindow(this);
            if (settingsWindow.ShowDialog() == true) // Открываем окно как модальное
            {
                // Получаем данные из дочернего окна
                //string message = childWindow.ResultMessage;
                IsCheckDB = settingsWindow.stateDB;
               // Execute(); // Вызываем метод который обновляет данные 
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
