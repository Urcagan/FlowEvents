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
            _pathDB = App.Settings.pathDB; //_MainViewModel.appSettings.pathDB;
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
                if (!CheckDB.IsDatabaseVersionCorrect(App.Settings.VerDB, _connectionString)) //проверка версии базы данных
                {
                    MessageBox.Show($"Версия БД не соответствует требуемой версии {App.Settings.VerDB}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return ;
                }

                _pathDB = newPathDB;
                App.Settings.pathDB = _pathDB;
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
            
            App.Settings.SaveSettingsApp(); 
            this.DialogResult = true;

            _MainViewModel.UpdateConnectionString(_pathDB);
            _MainViewModel.LoadEvents();
        }
    }
}
