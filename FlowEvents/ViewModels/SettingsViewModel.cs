using FlowEvents.Services;
using FlowEvents.Services.Interface;
using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FlowEvents.Settings
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseValidationService _validationService;

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

        public SettingsViewModel(IDatabaseValidationService validationService )
        {
            _validationService = validationService;
          
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

        private async void FileDialogToPathDB(object parameters)
        {
            // Открываем диалог выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл базы данных",
                Filter = "Файлы базы данных (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*" // Настроить фильтрацию файлов по типу, если нужно
            };

            //string newPathDB = string.Empty; // Переменная для хранения нового пути к базе данных
            if (openFileDialog.ShowDialog() == true)
            {
                // Обновляем путь в настройках
                var newPath = openFileDialog.FileName;

                var result = await _validationService.ValidateDatabaseAsync(newPath, App.Settings.VerDB);
                //ПОСЛЕ этой строки можно использовать result.IsValid и result.Message
                // и если result.IsValid true , то база данных валидна и можно обновить настройки


                if (!CheckDB.DBGood(newPath)) return;
                            
                PathToDB = newPath; //Вывод пути в текстовое поле окна.

                App.Settings.pathDB = newPath;
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
