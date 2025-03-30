using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Windows.Shapes;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly MainViewModel _MainViewModel;

        private string _pathDB;
        private string newPathDB;

        public SettingsWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _MainViewModel = mainViewModel;
            _pathDB = _MainViewModel.appSettings.pathDB;
            // Загружаем текущий путь к базе данных в текстовое поле
           // FilePathTextBox.Text = _pathDB;
        }

        public bool stateDB { get; private set; }


        // Обработчик события для кнопки "Выбрать файл"
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалог выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл базы данных",
                Filter = "Файлы базы данных (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*" // Настроить фильтрацию файлов по типу, если нужно
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Обновляем путь в настройках
                newPathDB = openFileDialog.FileName;

                if (!CheckDB.CheckPathToFileDB(newPathDB)) return ;   // Проверяем путь к базе данных и выходим, если он неверен
                                
                string _connectionString = $"Data Source={newPathDB};Version=3;foreign keys=true;"; //Формируем сторку подключения к БД

                // Проверка версии базы данных
                if (!CheckDB.IsDatabaseVersionCorrect(_MainViewModel.appSettings.VerDB, _connectionString))  //проверка версии базы данных
                {
                    MessageBox.Show($"Версия БД не соответствует требуемой версии {_MainViewModel.appSettings.VerDB}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return ;
                }

                // if (!CheckDB.CheckDatabaseFileVer(_pathDB, _MainViewModel.appSettings.VerDB)) return; //Проверяем версию БД

                _pathDB = newPathDB;
                _MainViewModel.appSettings.pathDB = _pathDB;
                FilePathTextBox.Text = _pathDB;
                _MainViewModel.FilePath = _pathDB;
            }
            else
            {
                MessageBox.Show("Программа не может продолжить без базы данных!", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                stateDB = false; // Файл не выбран
                                 // Application.Current.Shutdown(); // Закрываем приложение
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FilePathTextBox.Text = _pathDB;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _MainViewModel.appSettings.SaveSettingsApp();
            this.DialogResult = true;

            _MainViewModel.UpdateConnectionString(_pathDB);
            _MainViewModel.LoadEvents();
        }
    }
}
