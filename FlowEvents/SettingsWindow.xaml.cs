using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.IO;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _appSettings;
        private readonly MainViewModel _mainViewModel;

        private readonly MainWindow _MainWindow;

        public SettingsWindow(AppSettings appSettings, MainViewModel mainViewModel)
        {
            InitializeComponent();
            _appSettings = appSettings;
            _mainViewModel = mainViewModel;
            //_MainWindow = mainWindow;

            // Загружаем текущий путь к базе данных в текстовое поле
            FilePathTextBox.Text = _appSettings.pathDB;
        }

        public bool stateDB { get; private set; }

        // Обработчик события для кнопки "Выбрать файл..."
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Database files (*.db)|*.db|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(_appSettings.pathDB);

            if (openFileDialog.ShowDialog() == true)
            {
                // Обновляем текстовое поле с новым путем
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        /**
        // Обработчик события для кнопки "Выбрать файл"
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            AppSettings appSettings = new AppSettings();

            // Открываем диалог выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл базы данных",
                Filter = "Файлы базы данных (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*" // Настроить фильтрацию файлов по типу, если нужно
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Обновляем путь в настройках
                appSettings.pathDB = openFileDialog.FileName;
                Global_Var.pathDB = appSettings.pathDB;

                // изменить путь к базе данных и обновить соединение с новым путем.
                DatabaseHelper.ChangeDatabasePath(Global_Var.pathDB);

                if (CheckDB.ALLCheckDB(_MainWindow.databaseHelper, Global_Var.pathDB, "Config", appSettings.VerDB))
                {
                    appSettings.Save(); // Сохраняем обновленные настройки пути к БД
                    FilePathTextBox.Text = Global_Var.pathDB; 
                    MessageBox.Show("Новый путь к базе данных сохранен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    stateDB =true;
                }
                else
                {
                    MessageBox.Show("База данных не соответствует необходимым требованиям. Выберите другой файл!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    stateDB = false; // Файл выбран и путь обновлен
                }

            }
            else
            {
                MessageBox.Show("Программа не может продолжить без базы данных!", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                stateDB = false; // Файл не выбран
                        // Application.Current.Shutdown(); // Закрываем приложение
            }

        }
        **/

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FilePathTextBox.Text = Global_Var.pathDB;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            // Сохраняем новый путь к базе данных в настройках
            _appSettings.pathDB = FilePathTextBox.Text;
            _appSettings.Save();

            // Обновляем путь к базе данных в MainViewModel
     //       _mainViewModel.UpdateDatabasePath(_appSettings.pathDB);

            this.DialogResult = true;

            // Вызываем метод родительского окна который обновляет данные 
           // _MainWindow.Execute();
        }
    }
}
