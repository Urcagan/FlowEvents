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
        public SettingsWindow()
        {
            InitializeComponent();
        }

        // Обработчик события для кнопки "Выбрать файл"
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем новый диалог для выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // Настроить фильтрацию файлов по типу, если нужно
            openFileDialog.Filter = "Data Base (*.db)|*.db";


            // Если файл выбран, то выводим путь в TextBox
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }
    }
}
