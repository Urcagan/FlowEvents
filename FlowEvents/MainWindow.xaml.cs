using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppSettings appSettings;

        public DatabaseHelper databaseHelper;

        public MainWindow()
        {
            InitializeComponent();

            // Загружаем настройки при запуске программы
            appSettings = AppSettings.Load();

            //Необходимо проверить есть ли файл базы данных по указанному пути

            // Проверяем, существует ли база данных
            CheckDatabaseFile();
             Global_Var.pathDB = appSettings.pathDB;

            databaseHelper = new DatabaseHelper(Global_Var.pathDB);    // Инициализация копии класса работы с БД

            lblPath.Text = "Путь: " + Global_Var.pathDB; //Global_Var.pathDB;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            CheckStateDB();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
  
        }

        private void UnitWindow_Click(object sender, RoutedEventArgs e)
        {
            UnitsWindow unitsWindow = new UnitsWindow(databaseHelper); // Передаем объект в конструктор
            unitsWindow.ShowDialog();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Создаем и показываем окно настроек
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog(); // Открываем окно как модальное
        }

        // Проверка наличия файла БД
        private void CheckDatabaseFile()
        {
            // Проверяем, указан ли путь и существует ли файл
            if (string.IsNullOrWhiteSpace(appSettings.pathDB) || !File.Exists(appSettings.pathDB))
            {
                MessageBox.Show("Файл базы данных не найден. Укажите корректный путь.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Открываем диалог выбора файла
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Выберите файл базы данных",
                    Filter = "Файлы базы данных (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Обновляем путь в настройках
                    appSettings.pathDB = openFileDialog.FileName;
                    appSettings.Save(); // Сохраняем обновленные настройки

                    MessageBox.Show("Новый путь к базе данных сохранен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Программа не может продолжить без базы данных!", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown(); // Закрываем приложение
                }
            }
        }

        // Проверка БД на исправеность
        public void CheckStateDB()
        {
            bool stateDB;   // переменная состояния проверки базы данных
            // Проверка базы данных на исправность
            stateDB = CheckDB.ALLCheckDB(databaseHelper, Global_Var.pathDB, "VerDB", appSettings.VerDB);
        }

        
    }


}
