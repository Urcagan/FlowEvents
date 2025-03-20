using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow _MainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _MainWindow = mainWindow;

            // Загружаем текущий путь к базе данных в текстовое поле
           // FilePathTextBox.Text = -----------------
        }

        public bool stateDB { get; private set; }
        
        
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
                    appSettings.SaveSettingsApp(); // Сохраняем обновленные настройки пути к БД
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FilePathTextBox.Text = Global_Var.pathDB;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.DialogResult = true;

            // Вызываем метод родительского окна который обновляет данные 
           // _MainWindow.Execute();
        }
    }
}
