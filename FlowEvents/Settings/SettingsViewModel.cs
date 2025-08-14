using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using static FlowEvents.Global_Var;

namespace FlowEvents.Settings
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        internal MainViewModel _mainViewModel;

        private string _pathToDB ;
        public string PathToDB
        {
            get => _pathToDB;
            set
            {
                if (_pathToDB != value)
                {
                    _pathToDB = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _pathRelises;
        public string PathRelises
        {
            get => _pathRelises;
            set
            {
                if (_pathRelises != value)
                {
                    _pathRelises = value;

                    App.Settings.UpdateRepository =value ;
                    OnPropertyChanged(nameof(PathRelises));
                }
            }
        }

        public RelayCommand SetPathDBCommand { get; }
        public RelayCommand WindowClossingCommand { get; }

        public SettingsViewModel( MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            PathToDB = App.Settings.pathDB;
            PathRelises = App.Settings.UpdateRepository;

            SetPathDBCommand = new RelayCommand(FileDialogToPathDB);
            WindowClossingCommand = new RelayCommand(OnWindowsClosing);
        }


        private void OnWindowsClosing(object paramerts) // Обработчик закрытия окна настроек
        {
            //MessageBox.Show("Закрытие окна настроек. Настройки будут сохранены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

            //App.Settings.UpdateRepository = PathRelises; //Запись пути к репозиторию обновлений

            App.Settings.SaveSettingsApp();

            _mainViewModel.UpdateConnectionString(PathToDB);
            //_mainViewModel.LoadUnitsToComboBox();
            //_mainViewModel.LoadEvents();
            _mainViewModel.StartUP();

            if (paramerts is Window window)
            {
                window.Close();
            }


        }

        private void FileDialogToPathDB(object parameters)
        {
            // Открываем диалог выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл базы данных",
                Filter = "Файлы базы данных (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*" // Настроить фильтрацию файлов по типу, если нужно
            };

            string newPathDB = string.Empty; // Переменная для хранения нового пути к базе данных
            if (openFileDialog.ShowDialog() == true)
            {
                // Обновляем путь в настройках
                newPathDB = openFileDialog.FileName;

                if (!CheckDB.CheckPathToFileDB(newPathDB)) return;   // Проверяем путь к базе данных и выходим, если он неверен

                string _connectionString = $"Data Source={newPathDB};Version=3;foreign keys=true;"; //Формируем сторку подключения к БД

                // Проверка версии базы данных
                if (!CheckDB.IsDatabaseVersionCorrect(App.Settings.VerDB, _connectionString)) //проверка версии базы данных
                {
                    MessageBox.Show($"Версия БД не соответствует требуемой версии {App.Settings.VerDB}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                App.Settings.pathDB = newPathDB;
                PathToDB = newPathDB;
                _mainViewModel.FilePath = newPathDB;
            }
            else
            {
                MessageBox.Show("Программа не может продолжить без базы данных!", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                //stateDB = false; // Файл не выбран
                                 // Application.Current.Shutdown(); // Закрываем приложение
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }


}
