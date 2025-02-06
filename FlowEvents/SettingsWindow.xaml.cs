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

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        private DatabaseHelper _databaseHelper;

        public SettingsWindow(DatabaseHelper databaseHelper)
        {
            InitializeComponent();
            _databaseHelper = databaseHelper;
        }



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

                if (CheckDB.ALLCheckDB(_databaseHelper, Global_Var.pathDB, "Config", appSettings.VerDB))
                {                    
                    appSettings.Save(); // Сохраняем обновленные настройки пути к БД
                    MessageBox.Show("Новый путь к базе данных сохранен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }


                MessageBox.Show("База данных не соответствует необходимым требованиям. Выберите другой файл!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;//true; // Файл выбран и путь обновлен
            }
            else
            {
                MessageBox.Show("Программа не может продолжить без базы данных!", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                return; // false; // Файл не выбран
                        // Application.Current.Shutdown(); // Закрываем приложение
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FilePathTextBox.Text = Global_Var.pathDB;
        }
    }
}
