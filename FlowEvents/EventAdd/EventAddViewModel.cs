using FlowEvents.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Windows;

namespace FlowEvents
{
    public class EventAddViewModel : INotifyPropertyChanged
    {
        private MainViewModel _mainViewModel;
        public MainViewModel MainViewModel
        {
            get { return _mainViewModel; }
            set
            {
                _mainViewModel = value;
            }
        }

        private string _connectionString;
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
            }
        }

        public ObservableCollection<UnitModel> Units { get; set; } = new ObservableCollection<UnitModel>();

        public ObservableCollection<CategoryModel> Categories { get; set; } = new ObservableCollection<CategoryModel>();


        private CategoryModel _selectedCategory;
        public CategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        private DateTime _selectedDateEvent;
        public DateTime SelectedDateEvent
        {
            get { return _selectedDateEvent; }
            set
            {
                _selectedDateEvent = value;
                OnPropertyChanged();
            }
        }

        private string _refining;
        public string Refining
        {
            get { return _refining; }
            set
            {
                _refining = value;
                OnPropertyChanged();
            }
        }

        private int id_Category;
        public int Id_Category
        {
            get { return id_Category; }
            set
            {
                id_Category = value;
                OnPropertyChanged();
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        private string _action;
        public string Action
        {
            get { return _action; }
            set
            {
                _action = value;
                OnPropertyChanged();
            }
        }

        private string dateCteate;
        public string DateCreate
        {
            get { return dateCteate; }
            set
            {
                dateCteate = value;
                OnPropertyChanged();
            }
        }

        private string creator;
        public string Creator
        {
            get { return creator; }
            set { creator = value; }
        }

        private string _selectedUnitsText = "";
        public string SelectedUnitsText
        {
            get => _selectedUnitsText;
            set
            {
                _selectedUnitsText = value;
                OnPropertyChanged(nameof(SelectedUnitsText));
                OnPropertyChanged(nameof(CanSave));
            }
        }

        // Свойство с Id выбранных элементов
        public List<int> SelectedIds => Units.Where(u => u.IsSelected).Select(u => u.Id).ToList();

        //----------------------------------------

        private readonly CultureInfo culture = AppBaseConfig.culture;   // Культура русского языка
        private readonly string formatDate = AppBaseConfig.formatDate; //Формат даты в приложении
        private readonly string formatDateTime = AppBaseConfig.formatDateTime; // формат даты с временем

        public ObservableCollection<AttachedFileModel> AttachedFiles { get; } = new ObservableCollection<AttachedFileModel>();

        private string _storagePath; // = "C:\\temp\\Attachments";  // Путь для хранения файлов
        private long? _currentEventId;  // Будет установлен после сохранения события

        // Команды взаимодействия с view

        public RelayCommand AttachFileCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }



        public EventAddViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            ConnectionString = $"Data Source={_mainViewModel.appSettings.pathDB};Version=3;";

            AttachFileCommand = new RelayCommand(AttachFile);
            SaveCommand = new RelayCommand(SaveNewEvent);
            CancelCommand = new RelayCommand(Cancel);

            GetUnitFromDatabase(); //Получаем элементы УСТАНОВКА из БД

            Categories.Insert(0, new CategoryModel { Id = -1, Name = "Выбор события" });
            SelectedCategory = Categories.FirstOrDefault();
            GetCategoryFromDatabase();

            SelectedDateEvent = DateTime.Now; // Установка текущей даты по умолчанию

            // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически)
            foreach (var unit in Units)
            {
                unit.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(UnitModel.IsSelected))
                    {
                        UpdateSelectedUnitsText();
                        OnPropertyChanged(nameof(SelectedIds)); // Уведомляем об изменении SelectedI
                    }
                };
            }
            // Первоначальное обновление
            UpdateSelectedUnitsText();

        }

        /// <summary>
        /// Возвращает путь к папке "Attachments" в той же директории, где находится указанный файл.
        /// </summary>
        /// <param name="filePath">Полный путь к файлу.</param>
        /// <returns>Путь к папке Attachments.</returns>
        public string GetAttachmentsFolderPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            string basePath = Path.GetDirectoryName(filePath);
            string attachmentsFolder = "Attachments";

            return Path.Combine(basePath, attachmentsFolder);
        }

        public string GetAttachmentsFolderPath(string filePath, DateTime eventDate)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            string basePath = Path.GetDirectoryName(filePath);
            string attachmentsRoot = "Attachments";
            string yearFolder = eventDate.Year.ToString();
            string monthFolder = eventDate.Month.ToString("00"); // Форматируем с ведущим нулем

            // Комбинируем все части пути
            return Path.Combine(basePath, attachmentsRoot, yearFolder, monthFolder);
        }


        private async void AttachFile(object parameters)
        {
            var filePath = SelectFile();
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var fileInfo = new FileInfo(filePath);

                // Проверка размера файла (например, до 10MB)
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show("Файл слишком большой. Максимальный размер: 10 МБ.");
                    return;
                }

                // Гененрируем путь для хранения файла
                _storagePath = GetAttachmentsFolderPath(_mainViewModel.FilePath, SelectedDateEvent);
                
                // Генерируем уникальное имя файла
                string originalFileName = fileInfo.Name;
                string fileExtension = fileInfo.Extension;
                string uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                string storagePath = Path.Combine(_storagePath, uniqueFileName);

                // Проверяем, существует ли папка для хранения файлов
                if (!File.Exists(_storagePath))
                { 
                    Directory.CreateDirectory(_storagePath); // Создаем папку, если ее нет
                }

                // Копируем файл
                File.Copy(filePath, storagePath);

                // Добавляем запись в коллекцию (пока без EventId)
                AttachedFiles.Add(new AttachedFileModel
                {
                    FileName = fileInfo.Name,
                    FilePath = storagePath,
                    FileSize = fileInfo.Length,
                    FileType = fileExtension,
                    UploadDate = DateTime.Now
                });

                MessageBox.Show("Файл успешно прикреплен!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка прикрепления файла: {ex.Message}");
            }
        }

        public string SelectFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл",
                Filter = "Все файлы (*.*)|*.*|PDF (*.pdf)|*.pdf|Изображения (*.png;*.jpg)|*.png;*.jpg",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private void UpdateSelectedUnitsText()
        {
            var selectedUnits = Units.Where(u => u.IsSelected).Select(u => u.Unit);
            SelectedUnitsText = selectedUnits.Any()
                ? $"Выбрано: {string.Join(", ", selectedUnits)}"
                : "Ничего не выбрано";
        }

        //Сохранение новой записи 
        private void SaveNewEvent(object parameters)
        {
            if (!ValidateEvent()) return;
            var newEvent = new EventsModel // Создание экземпляра для хранения нового Event
            {
                //DateEvent = DatePicker.SelectedDate?.ToString(formatDate) ?? DateTime.Now.ToString(formatDate),
                DateEvent = SelectedDateEvent.ToString(formatDate),
                OilRefining = Refining,
                Id_Category = _selectedCategory.Id,
                Description = Description,
                Action = Action,
                DateCreate = DateTime.Now.ToString(formatDateTime, culture), // Получить текущую дату и время
                Creator = "Автор"
            };

            //AddEvent(newEvent);
            // Сохраняем событие и получаем его ID
            _currentEventId = AddEventAndGetId(newEvent);

            // Сохраняем прикрепленные файлы в БД
            if (_currentEventId.HasValue)
            {
                SaveAttachedFilesToDatabase(_currentEventId.Value);
            }

            CloseWindow();
        }

        //Валидация перед сохранением:
        private bool ValidateEvent()
        {
            if (SelectedIds == null || SelectedIds.Count == 0) // Проверка выбранной установки
            {
                MessageBox.Show("Не выбрана установка");
                return false;
            }

            if (SelectedCategory == null || SelectedCategory.Id == -1) // Проверка выбранной категории
            {
                MessageBox.Show("Не выбрана категория");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Description)) // Проверка описания события
            {
                MessageBox.Show("Не заполнено описание события");
                return false;
            }
            return true;
        }


        private void GetUnitFromDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @" Select id, Unit, Description From Units ";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Units.Add(new UnitModel
                            {
                                Id = reader.GetInt32(0),
                                Unit = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void GetCategoryFromDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @" Select id, Name, Description, Colour From Category ";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Categories.Add(new CategoryModel
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Colour = reader.IsDBNull(3) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }



        /// <summary>
        /// Добавление новой строки в таблицу задач 
        /// </summary>
        /// <param name="newEvent"></param>
        public void AddEvent(EventsModel newEvent)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Выполнение вставки записи в таблицу Tasks
                            long eventId = InsertEvent(connection, newEvent);

                            // Добавление записей в таблицу TaskUnits для связывания с элементами ListView
                            foreach (long unitId in SelectedIds)
                            {
                                InsertEventUnit(connection, eventId, unitId);
                            }

                            // Подтвердить транзакцию
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            // В случае ошибки откатить транзакцию
                            transaction.Rollback();
                            MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
                        }
                    }
                }
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}");
            }

        }

        private long AddEventAndGetId(EventsModel newEvent)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        long eventId = InsertEvent(connection, newEvent);

                        foreach (long unitId in SelectedIds)
                        {
                            InsertEventUnit(connection, eventId, unitId);
                        }

                        transaction.Commit();
                        return eventId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Вставка нового события в базу данных
        /// </summary>
        /// <param name="connection"> Открытое соединение с базой данных</param>
        /// <param name="newEvent"> Модель с данными новой записи </param>
        /// <returns></returns>
        private long InsertEvent(SQLiteConnection connection, EventsModel newEvent)
        {
            // SQL-запрос для вставки данных
            var query = @"
                INSERT INTO Events (DateEvent, OilRefining, id_category, Description, Action, DateCreate, Creator)
                VALUES (@DateEvent, @OilRefining, @id_category, @Description, @Action, @DateCreate, @Creator);";

            using (var command = new SQLiteCommand(query, connection))
            {
                // Добавление параметров
                command.Parameters.AddWithValue("@DateEvent", newEvent.DateEvent);
                command.Parameters.AddWithValue("@OilRefining", newEvent.OilRefining ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@id_category", newEvent.Id_Category);
                command.Parameters.AddWithValue("@Description", newEvent.Description ?? (object)DBNull.Value); // Если Description == null, вставляем NULL
                command.Parameters.AddWithValue("@Action", newEvent.Action ?? (object)DBNull.Value); // Если Action == null, вставляем NULL
                command.Parameters.AddWithValue("@DateCreate", newEvent.DateCreate);
                command.Parameters.AddWithValue("@Creator", newEvent.Creator);

                // Выполнение запроса
                command.ExecuteNonQuery();
            }
            return connection.LastInsertRowId;

        }


        /// <summary>
        /// Добавление записей в таблицу EventUnits для связывания записей Event и Units
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="unitId"></param>
        private void InsertEventUnit(SQLiteConnection connection, long eventId, long unitId)
        {
            string insertTaskUnitsQuery = "INSERT INTO EventUnits (EventID, UnitID) VALUES (@EventID, @UnitID);";
            using (SQLiteCommand command = new SQLiteCommand(insertTaskUnitsQuery, connection))
            {
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@UnitID", unitId);
                command.ExecuteNonQuery();
            }
        }

        private void SaveAttachedFilesToDatabase(long eventId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                foreach (var file in AttachedFiles)
                {
                    string query = @"
                INSERT INTO AttachedFiles 
                (EventId, FileName, FilePath, FileSize, FileType, UploadDate)
                VALUES 
                (@EventId, @FileName, @FilePath, @FileSize, @FileType, @UploadDate)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EventId", eventId);
                        command.Parameters.AddWithValue("@FileName", file.FileName);
                        command.Parameters.AddWithValue("@FilePath", file.FilePath);
                        command.Parameters.AddWithValue("@FileSize", file.FileSize);
                        command.Parameters.AddWithValue("@FileType", file.FileType);
                        command.Parameters.AddWithValue("@UploadDate", file.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"));

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        // 
        private void LoadAttachedFiles(int eventId)
        {
            AttachedFiles.Clear();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM AttachedFiles WHERE EventId = @EventId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EventId", eventId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AttachedFiles.Add(new AttachedFileModel
                            {
                                FileId = reader.GetInt32(0),
                                EventId = reader.GetInt32(1),
                                FileName = reader.GetString(2),
                                FilePath = reader.GetString(3),
                                FileSize = reader.GetInt64(4),
                                FileType = reader.GetString(5),
                                UploadDate = DateTime.Parse(reader.GetString(6))
                            });
                        }
                    }
                }
            }
        }

        public bool CanSave //свойство, которое проверяет все обязательные поля:
        {
            get
            {
                return SelectedIds != null && SelectedIds.Count > 0 &&
                       SelectedCategory != null && SelectedCategory.Id != -1 &&
                       !string.IsNullOrWhiteSpace(Description);
            }
        }


        private void Cancel(object parameter)
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?
                .Close();
        }

        //-------------------------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
