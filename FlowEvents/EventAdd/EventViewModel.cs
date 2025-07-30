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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class EventViewModel : INotifyPropertyChanged
    {
        private MainViewModel _mainViewModel;
        public readonly EventsModelForView _originalEvent;
        private readonly string _connectionString;
        
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
            get => _selectedDateEvent; 
            set
            {
                _selectedDateEvent = value;
                OnPropertyChanged();
            }
        }

        private string _refining;
        public string Refining
        {
            get => _refining;
            set
            {
                _refining = value;
                OnPropertyChanged();
            }
        }

        private int _idCategory;
        public int Id_Category
        {
            get => _idCategory;
            set
            {
                _idCategory = value;
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
            get => _action;
            set
            {
                _action = value;
                OnPropertyChanged();
            }
        }

        private string _dateCteate;
        public string DateCreate
        {
            get => _dateCteate;
            set
            {
                _dateCteate = value;
                OnPropertyChanged();
            }
        }

        private string _creator;
        public string Creator
        {
            get => _creator;        
            set => _creator = value; 
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

        private int _edtedEventId; // Идентификатор редактируемого события, если есть
        public ObservableCollection<UnitModel> Units { get; set; } = new ObservableCollection<UnitModel>();
        public ObservableCollection<CategoryModel> Categories { get; set; } = new ObservableCollection<CategoryModel>
        {
            new CategoryModel { Id = -1, Name = "Выбор события" }
        };

        // Свойство с Id выбранных элементов
        public List<int> SelectedIds => Units.Where(u => u.IsSelected).Select(u => u.Id).ToList();

        //----------------------------------------

        private readonly CultureInfo culture = AppBaseConfig.culture;   // Культура русского языка
        private readonly string formatDate = AppBaseConfig.formatDate; //Формат даты в приложении
        private readonly string formatDateTime = AppBaseConfig.formatDateTime; // формат даты с временем

        public ObservableCollection<AttachedFileModel> AttachedFiles { get; } = new ObservableCollection<AttachedFileModel>();
        
        private string _storagePath; // = "C:\\temp\\Attachments";  // Путь для хранения файлов
        private long? _currentEventId;  // Будет установлен после сохранения события


        // Команды 
        public RelayCommand AttachFileCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }


        // Конструктор класса инициализации ViewModel для добавления события
        public EventViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _connectionString = _mainViewModel._connectionString;  //$"Data Source={_mainViewModel.appSettings.pathDB};Version=3;";

            AttachFileCommand = new RelayCommand(AttachFile);
            SaveCommand = new RelayCommand(SaveNewEvent);
            CancelCommand = new RelayCommand(Cancel);

            GetUnitFromDatabase(); //Получаем элементы УСТАНОВКА из БД

            SelectedCategory = Categories.FirstOrDefault(); // Установка категории по умолчанию
            GetCategoryFromDatabase();

            SelectedDateEvent = DateTime.Now; // Установка текущей даты по умолчанию

            // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически)
            SubscribeToUnitsPropertyChanged();            
            //UpdateSelectedUnitsText();
        }


        // Конструктор класса инициализации ViewModel для РЕДАКТИРОВАНИЯ события
        public EventViewModel(MainViewModel mainViewModel, EventsModelForView eventToEdit)
        {
            _mainViewModel = mainViewModel;
            _originalEvent = eventToEdit;
            _connectionString = _mainViewModel._connectionString; //$"Data Source={_mainViewModel.appSettings.pathDB};Version=3;";

            AttachFileCommand = new RelayCommand(AttachFile);
            SaveCommand = new RelayCommand(SaveUpdatedEvents);
            CancelCommand = new RelayCommand(Cancel);

            _edtedEventId = _originalEvent.Id; // Сохраняем ID редактируемого события
            SelectedDateEvent = _originalEvent.DateEvent; // Установка даты события
            Refining = _originalEvent.OilRefining;
            Description = _originalEvent.Description;  
            Action = _originalEvent.Action;

            GetUnitFromDatabase(); //Получаем элементы УСТАНОВКА из БД
            SubscribeToUnitsPropertyChanged(); // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически)
        }


        private void SubscribeToUnitsPropertyChanged() // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически)
        {
            foreach (var unit in Units)
            {
                unit.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(UnitModel.IsSelected))
                    {
                        UpdateSelectedUnitsText();
                        OnPropertyChanged(nameof(SelectedIds));
                    }
                };
            }
        }


        // Асинхронная инициализация (Запуск происходит в EventWindows.xaml.cs)
        public async Task InitializeAsync()
        {
            // здесь запускаем все асинхронные операции коорые надо выполнять в момент открытия окна
            GetCategoryFromDatabase(); // Получаем категории из БД
            SelectedCategory = Categories.FirstOrDefault(c => c.Name == _originalEvent.Category);
            await LoadSelectedUnitsForEvent(_edtedEventId); // Получение перечень установок связанных с этим событием
            LoadAttachedFiles(_edtedEventId); // Получаем перечень прикрепленных файлов.
        }


        // Востанавливаем выбор на элементах в окне установок
        public async Task LoadSelectedUnitsForEvent(int eventId)
        {
            // 1. Получаем список UnitID, связанных с этим EventID из базы
            List<int> selectedUnitIds = await GetUnitIdsForEvent(eventId);

            // 2. Обновляем флаги IsSelected в коллекции Units
            foreach (var unit in Units)
            {
                unit.IsSelected = selectedUnitIds.Contains(unit.Id);
            }
        }

        // Возвращает список UnitID для данного EventID
        private async Task<List<int>> GetUnitIdsForEvent(int eventId)
        {
            var unitIds = new List<int>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(
                    "SELECT UnitID FROM EventUnits WHERE EventID = @eventId",
                    connection))
                {
                    command.Parameters.AddWithValue("@eventId", eventId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            unitIds.Add(reader.GetInt32(0));
                        }
                    }
                }
            }
            return unitIds;
        }

        public void RemoveAttachedFile(AttachedFileModel deletedFile) // метод для удаления файла из коллекции
        {
            if (AttachedFiles.Contains(deletedFile))
            {
                deletedFile.FileDeleted -= RemoveAttachedFile; // Отписываемся от события т.к. удаляем запись
                // Удаляем файл из коллекции
                AttachedFiles.Remove(deletedFile);
            }
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
            var filePath = SelectFile(); // Вызываем диалоговое окно для выбора файла
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
                                
                var newFile = new AttachedFileModel(_connectionString) // Создаем модель файла
                {
                    FileName = fileInfo.Name,
                    FilePath = storagePath,
                    FileSize = fileInfo.Length,
                    FileType = fileExtension,
                    UploadDate = DateTime.Now
                };

                // Подписываемся на событие удаления файла
                newFile.FileDeleted += RemoveAttachedFile; //Подписываем событие FileDeleted на выполнение метода RemoveAttachedFile;
                // Добавляем запись в коллекцию (пока без EventId)
                AttachedFiles.Add(newFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка прикрепления файла: {ex.Message}");
            }
        }

        // Диалоговое окно для выбора файла
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
                DateEvent = SelectedDateEvent.ToString(formatDate),
                OilRefining = Refining,
                Id_Category = _selectedCategory.Id,
                Description = Description,
                Action = Action,
                DateCreate = DateTime.Now.ToString(formatDateTime, culture), // Получить текущую дату и время
                Creator = "Автор"
            };

            // Сохраняем событие и получаем его ID
            _currentEventId = AddEventAndGetId(newEvent);

            // Сохраняем прикрепленные файлы в БД
            if (_currentEventId.HasValue)
            {
                SaveAttachedFilesToDatabase(_currentEventId.Value);
            }

            CloseWindow();
        }

        private void SaveUpdatedEvents(object parameters)
        {
            if (!ValidateEvent()) return;
            // Создание экземпляра для хранения нового Event
            var _updateEvent = new EventsModel
            {
                Id = this._edtedEventId,
                DateEvent = SelectedDateEvent.ToString(AppBaseConfig.formatDate),
                OilRefining = Refining,
                Id_Category = _selectedCategory.Id,
                Description = Description,
                Action = Action
            };
            EditEvent(_updateEvent, SelectedIds);

            // Сохраняем прикрепленные файлы в БД

            SaveAttachedFilesToDatabase(_updateEvent.Id);
            _currentEventId = _updateEvent.Id;

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
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
            }
        }



        /// <summary>
        /// Добавление новой строки в таблицу задач 
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
                        InsertEventUnit(connection, eventId, SelectedIds);
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
                command.ExecuteNonQuery();
            }
            return connection.LastInsertRowId;
        }


        /// <summary>
        /// Добавление записей в таблицу EventUnits для связывания записей Event и Units
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="unitId"></param>
        private void InsertEventUnit(SQLiteConnection connection, long eventId, List<int> unitIds)
        {
            string insertTaskUnitsQuery = "INSERT INTO EventUnits (EventID, UnitID) VALUES (@EventID, @UnitID);";
            foreach (long unitId in unitIds)
            {
                using (SQLiteCommand command = new SQLiteCommand(insertTaskUnitsQuery, connection))
                {
                    command.Parameters.AddWithValue("@EventID", eventId);
                    command.Parameters.AddWithValue("@UnitID", unitId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void EditEvent(EventsModel updateEvent, List<int> selectedIds)
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
                            UpdateEvent(connection, updateEvent); // Обновление записи события

                            UpdateEventUnit(connection, _edtedEventId, selectedIds); // Обновление данных по объектам

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

        private void UpdateEvent(SQLiteConnection connection, EventsModel updateEvent) // SQL-запрос для обновления данных
        {
            var query = "UPDATE Events SET DateEvent = @DateEvent, OilRefining = @OilRefining , id_category = @id_category, Description = @Description, Action = @Action WHERE id = @SelectedRowId ";

            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@DateEvent", updateEvent.DateEvent);
                command.Parameters.AddWithValue("@OilRefining", updateEvent.OilRefining ?? (object)DBNull.Value); // Если OilRefining == null, вставляем NULL
                command.Parameters.AddWithValue("@id_category", updateEvent.Id_Category);
                command.Parameters.AddWithValue("@Description", updateEvent.Description ?? (object)DBNull.Value); // Если Description == null, вставляем NULL
                command.Parameters.AddWithValue("@Action", updateEvent.Action ?? (object)DBNull.Value); // Если Action == null, вставляем NULL
                command.Parameters.AddWithValue("@SelectedRowId", updateEvent.Id);
                command.ExecuteNonQuery();
            }
        }

        private void UpdateEventUnit(SQLiteConnection connection, long eventId, List<int> unitIds)
        {
            // Удаляем все существующие связи для данной задачи
            var deleteQuery = "DELETE FROM EventUnits WHERE EventID = @EventId;";
            using (var command = new SQLiteCommand(deleteQuery, connection))
            {
                command.Parameters.AddWithValue("@EventId", eventId);
                command.ExecuteNonQuery();
            }
            // Вставляем новые связи
            InsertEventUnit(connection, eventId, unitIds);
        }


        private void SaveAttachedFilesToDatabase(long eventId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                foreach (var file in AttachedFiles)
                {
                    string query = @" INSERT INTO AttachedFiles (EventId, FileName, FilePath, FileSize, FileType, UploadDate)
                                      VALUES (@EventId, @FileName, @FilePath, @FileSize, @FileType, @UploadDate)";

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
                            var loadFile = new AttachedFileModel(_connectionString) // Создаем экземпляр модели для загрузки файлов
                            {
                                FileId = reader.GetInt32(0),
                                EventId = reader.GetInt32(1),
                                FileName = reader.GetString(2),
                                FilePath = reader.GetString(3),
                                FileSize = reader.GetInt64(4),
                                UploadDate = DateTime.Parse(reader.GetString(5)),
                                FileType = reader.GetString(6)                                
                            };
                            // Подписываемся на событие удаления файла
                            loadFile.FileDeleted += RemoveAttachedFile;
                            AttachedFiles.Add(loadFile); // Добавляем файл в коллекцию
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
            // Если событие не сохранено (_currentEventId == null), удаляем файлы
            if (!_currentEventId.HasValue)
            {
                foreach (var file in AttachedFiles.ToList()) // ToList() для копирования коллекции
                {
                    try
                    {
                        if (File.Exists(file.FilePath))
                            File.Delete(file.FilePath);
                    }
                    catch { /* Игнорируем ошибки */ }
                }
            }

            // Закрытие окна, в котором находится этот ViewModel
            Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?
                .Close();
        }

        private void DeleteAttachedFile(AttachedFileModel file)
        {
            if (file != null) return;
            // Удаляем файл из коллекции
            AttachedFiles.Remove(file);
            // Удаляем файл с диска
            try
            {
                if (File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении файла: {ex.Message}");
            }
        }

        //-------------------------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
