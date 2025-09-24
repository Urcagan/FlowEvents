using FlowEvents.Services;
using FlowEvents.Services.Interface;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents.Settings
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IConnectionStringProvider _connectionProvider;
        private readonly IDatabaseValidationService _validationService;

        private string _pathToDB;
        private string _pathRelises;
        private string _statusMessage;
        private bool _isLoading;

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

        public string PathRelises
        {
            get => _pathRelises;
            set
            {
                if (_pathRelises != value)
                {
                    _pathRelises = value;

                    App.Settings.UpdateRepository = value;
                    OnPropertyChanged(nameof(PathRelises));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public RelayCommand SetPathDBCommand { get; }
        public RelayCommand WindowClossingCommand { get; }

        public SettingsViewModel(IDatabaseValidationService validationService, IConnectionStringProvider connectionProvider)
        { 
            _connectionProvider = connectionProvider;
            _validationService = validationService;

            PathToDB = App.Settings.pathDB;
            PathRelises = App.Settings.UpdateRepository;

            //SetPathDBCommand = new RelayCommand(FileDialogToPathDB);
            SetPathDBCommand = new RelayCommand(async () => await FileDialogToPathDBAsync());
            WindowClossingCommand = new RelayCommand(OnWindowsClosing);
        }


        private async Task FileDialogToPathDBAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл базы данных",
                Filter = "Файлы базы данных (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                ShowMessage("Файл базы данных не выбран", MessageType.Warning);
                return;
            }

            var newPath = openFileDialog.FileName;
            await ValidateAndSetDatabasePathAsync(newPath);
        }


        private async Task ValidateAndSetDatabasePathAsync(string newPath)
        {
            IsLoading = true;
            StatusMessage = "Проверка базы данных...";

            try
            {
                var result = await _validationService.ValidateDatabaseAsync(newPath, App.Settings.VerDB);

                if (!result.IsValid)
                {
                    ShowMessage($"Ошибка валидации: {result.Message}", MessageType.Error);
                    return;
                }

                // Обновляем настройки строки подключения
                var newConnectionString = $"Data Source={newPath};Version=3;foreign keys=true;";
                _connectionProvider.UpdateConnectionString(newConnectionString);

                App.Settings.pathDB = newPath; // Обновляем пут к базе данных в глобальной переменной для дальнейшего сохранения в файле конфигурации
                PathToDB = result.DatabaseInfo.Path;
                //   _appSettings.pathDB = result.DatabaseInfo.Path;

                ShowMessage("База данных успешно проверена и установлена", MessageType.Success);
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка при проверке базы данных: {ex.Message}", MessageType.Error);
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        private void ShowMessage(string message, MessageType messageType)
        {
            StatusMessage = message;

            var image = messageType switch
            {
                MessageType.Error => MessageBoxImage.Error,
                MessageType.Warning => MessageBoxImage.Warning,
                MessageType.Success => MessageBoxImage.Information,
                _ => MessageBoxImage.Information
            };

            MessageBox.Show(message, "Настройки", MessageBoxButton.OK, image);
        }


        private void OnWindowsClosing(object paramerts) // Обработчик закрытия окна настроек
        {
            App.Settings.SaveSettingsApp(); // Сохранение конфигурации приложения в файле cfg

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

                if (!result.IsValid) return;

                PathToDB = result.DatabaseInfo.Path;
                App.Settings.pathDB = result.DatabaseInfo.Path;

                //if (!CheckDB.DBGood(newPath)) return;

                //PathToDB = newPath; //Вывод пути в текстовое поле окна.

                //App.Settings.pathDB = newPath;
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

    public enum MessageType // Перечеь графичкских значков для метода MessageBox
    {
        Info,
        Warning,
        Error,
        Success
    }


}
