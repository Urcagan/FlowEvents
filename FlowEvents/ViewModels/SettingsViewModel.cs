using FlowEvents.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FlowEvents.Settings
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;

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

        public SettingsViewModel(IDatabaseService databaseService )
        {
            _databaseService = databaseService;
          
            PathToDB = App.Settings.pathDB;
            PathRelises = App.Settings.UpdateRepository;

            SetPathDBCommand = new RelayCommand(FileDialogToPathDB);
            WindowClossingCommand = new RelayCommand(OnWindowsClosing);
        }


        private void OnWindowsClosing(object paramerts) // Обработчик закрытия окна настроек
        {
            App.Settings.SaveSettingsApp();
                      
            //_mainViewModel.StartUP();

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

                if (!CheckDB.DBGood(newPathDB)) return;
                            
                PathToDB = newPathDB; //Вывод пути в текстовое поле окна.

                App.Settings.pathDB = newPathDB;
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
