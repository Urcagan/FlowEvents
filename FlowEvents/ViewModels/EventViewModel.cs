using FlowEvents.Models;
using FlowEvents.Models.Enums;
using FlowEvents.Repositories.Interface;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class EventViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        private readonly string _userName;
        private Category _selectedCategory;
        private DateTime _selectedDateEvent;
        private string _refining;
        private int _idCategory;
        private string _description;
        private string _action;
        private string _dateCreate;
        private string _creator;
        private string _selectedUnitsText = "";
        private int _editedEventId;                 // Идентификатор редактируемого события, если есть
        private string _storagePath;                // = "C:\\temp\\Attachments";  // Путь для хранения файлов
        private long? _currentEventId;              // Будет установлен после сохранения события


        private readonly EventForView _originalEvent;

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public DateTime SelectedDateEvent
        {
            get => _selectedDateEvent;
            set
            {
                _selectedDateEvent = value;
                OnPropertyChanged();
            }
        }

        public string Refining
        {
            get => _refining;
            set
            {
                _refining = value;
                OnPropertyChanged();
            }
        }

        public int IdCategory
        {
            get => _idCategory;
            set
            {
                _idCategory = value;
                OnPropertyChanged();
            }
        }

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

        public string Action
        {
            get => _action;
            set
            {
                _action = value;
                OnPropertyChanged();
            }
        }

        public string DateCreate
        {
            get => _dateCreate;
            set
            {
                _dateCreate = value;
                OnPropertyChanged();
            }
        }

        public string Creator
        {
            get => _creator;
            set => _creator = value;
        }

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

        public ObservableCollection<Unit> Units { get; set; } = new ObservableCollection<Unit>();
        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>
        {
            new Category { Id = -1, Name = "Выбор события" }
        };

        // Свойство с Id выбранных элементов
        public IEnumerable<int> SelectedIds => Units.Where(u => u.IsSelected).Select(u => u.Id).ToList();

        //----------------------------------------

        private readonly CultureInfo culture = AppBaseConfig.culture;   // Культура русского языка
        private readonly string formatDate = AppBaseConfig.formatDate; //Формат даты в приложении
        private readonly string formatDateTime = AppBaseConfig.formatDateTime; // формат даты с временем

        public ObservableCollection<AttachedFileModel> AttachedFilesDocument { get; } = new ObservableCollection<AttachedFileModel>();
        public ObservableCollection<AttachedFileModel> AttachedFilesMonitoring { get; } = new ObservableCollection<AttachedFileModel>();


        // Команды 
        public RelayCommand AttachFileCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private readonly IUnitRepository _unitRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IEventUnitRepository _eventUnitRepository;
        private readonly IEventRepository _eventRepository;


        // Конструктор ViewModel для Добавлениия события
        public EventViewModel(IUnitRepository unitRepository, ICategoryRepository categoryRepository,IEventRepository  eventRepository )
        {
           
            
            _unitRepository = unitRepository;
            _categoryRepository = categoryRepository;       
            _eventRepository = eventRepository;

            _connectionString = Global_Var.ConnectionString;  
            _userName = Global_Var.UserName; 

            AttachFileCommand = new RelayCommand(AttachFile);
            SaveCommand = new RelayCommand(AddEvent);
            CancelCommand = new RelayCommand(Cancel);

            _ = InitializeAsync(); // Первоначальная инициализация перечней объектов и категорий

            SelectedCategory = Categories.FirstOrDefault(); // Установка категории по умолчанию

            SelectedDateEvent = DateTime.Now; // Установка текущей даты по умолчанию

            // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически)
            SubscribeToUnitsPropertyChanged();
            //UpdateSelectedUnitsText();
        }


        // Конструктор ViewModel для РЕДАКТИРОВАНИЯ события
        //public EventViewModel(MainViewModel mainViewModel, EventForView eventToEdit)
        public EventViewModel(EventForView eventToEdit, IUnitRepository unitRepository, ICategoryRepository categoryRepository,IEventUnitRepository eventUnitRepository, IEventRepository eventRepository)
        {
            _unitRepository = unitRepository;
            _categoryRepository = categoryRepository;
            _eventUnitRepository = eventUnitRepository;
            _eventRepository = eventRepository;

            _originalEvent = eventToEdit;
            _connectionString = Global_Var.ConnectionString;

            AttachFileCommand = new RelayCommand(AttachFile);
            SaveCommand = new RelayCommand(UpdatedEvent);
            CancelCommand = new RelayCommand(Cancel);

            _editedEventId = _originalEvent.Id; // Сохраняем ID редактируемого события
            SelectedDateEvent = _originalEvent.DateEvent; // Установка даты события
            Refining = _originalEvent.OilRefining;
            Description = _originalEvent.Description;
            Action = _originalEvent.Action;


            // Запускаем инициализацию без блокировки конструктора
            // Первоначальная инициализация перечней объектов и категорий
            // Ждем завершения инициализации перед установкой SelectedCategory
            InitializeAsync().ContinueWith(t =>
            {
                // Этот код выполнится после завершения InitializeAsync
                SelectedCategory = Categories.FirstOrDefault(c => c.Name == _originalEvent.Category);
            }, TaskScheduler.FromCurrentSynchronizationContext());
                       
            LoadSelectedUnitsForEvent(_editedEventId); // Получение перечень установок связанных с этим событием
            
            LoadAttachedFiles(_editedEventId); // Получаем перечень прикрепленных файлов.

            SubscribeToUnitsPropertyChanged(); // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически)
        }


        private void SubscribeToUnitsPropertyChanged() // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически)
        {
            foreach (var unit in Units)
            {
                unit.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Unit.IsSelected))
                    {
                        UpdateSelectedUnitsText();
                        OnPropertyChanged(nameof(SelectedIds));
                    }
                };
            }
        }



        // Асинхронная инициализация элементов управления Обьектов и Категорий
        private async Task InitializeAsync() // здесь запускаем все асинхронные операции коорые надо выполнять в момент открытия окна
        {
            try
            {
                // 1. Получение перечня категорий для комбобокса из Category
                var categories = await _categoryRepository.GetAllCategoriesAsync();
                foreach (var category in categories) // Добавляем категории из БД в комбобокс категорий
                {
                    Categories.Add(category);
                }

                // 2. Получение перечня всех объектов из Units
                var units = await _unitRepository.GetAllUnitsAsync();
                foreach (var unit in units)
                {
                    Units.Add(unit);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Debug.WriteLine($"InitializeAsync error: {ex.Message}");
            }
        }



        // Востанавливаем выбор на элементах в окне установок
        public async Task LoadSelectedUnitsForEvent(int eventId)
        {
            // 1. Получаем список UnitID, связанных с этим EventID из базы
            List<int> selectedUnitIds = await _eventUnitRepository.GetIdUnitForEventAsync(eventId);  

            // 2. Обновляем флаги IsSelected в коллекции Units
            foreach (var unit in Units)
            {
                unit.IsSelected = selectedUnitIds.Contains(unit.Id);
            }
        }


        public void RemoveAttachedFile(AttachedFileModel fileToRemove) // метод для удаления файла из коллекции
        {
            if (AttachedFilesDocument.Contains(fileToRemove) && fileToRemove.Status == FileStatus.Deleted)
            {
                // deletedFile.FileDeleted -= RemoveAttachedFile; // Отписываемся от события т.к. удаляем запись
                // Удаляем файл из коллекции
                //AttachedFiles.Remove(deletedFile);
                fileToRemove.MarkAsDeleted(); // помечаем файл как "удаленный"

                // Можно добавить визуальное выделение в UI
                OnPropertyChanged(nameof(AttachedFilesDocument)); // Уведомляем об изменении коллекции
            }

            if (AttachedFilesMonitoring.Contains(fileToRemove) && fileToRemove.Status == FileStatus.Deleted)
            {
                fileToRemove.MarkAsDeleted(); // помечаем файл как "удаленный"
                OnPropertyChanged(nameof(AttachedFilesMonitoring)); // Уведомляем об изменении коллекции
            }
        }

        /// <summary>
        /// Возвращает путь к папке "Attachments" в той же директории, где находится указанный файл.
        /// </summary>
        /// <param name="filePath">Полный путь к файлу.</param>
        /// <returns>Путь к папке Attachments.</returns>
        //public string GetAttachmentsFolderPath(string filePath)
        //{
        //    if (string.IsNullOrEmpty(filePath))
        //        throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        //    string basePath = Path.GetDirectoryName(filePath);
        //    string attachmentsFolder = "Attachments";

        //    return Path.Combine(basePath, attachmentsFolder);
        //}

        public string GetAttachmentsFolderPath(DateTime eventDate)
        {
            string filePath = Global_Var.pathToDB;
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            string basePath = Path.GetDirectoryName(filePath);
            string attachmentsRoot = "Attachments";
            string yearFolder = eventDate.Year.ToString();
            string monthFolder = eventDate.Month.ToString("00"); // Форматируем с ведущим нулем

            // Комбинируем все части пути
            return Path.Combine(basePath, attachmentsRoot, yearFolder, monthFolder);
        }


        private void AttachFile(object parameter)
        {
            var fileCategory = parameter as string;

            string filePath = SelectFile(); // Вызываем диалоговое окно для выбора файла
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var fileInfo = new FileInfo(filePath);

                // Проверка размера файла (например, до 10MB)
                if (fileInfo.Length > App.Settings.SizeFileAttachment * 1024 * 1024)
                {
                    MessageBox.Show($"Файл слишком большой. Максимальный размер: {App.Settings.SizeFileAttachment} МБ.");
                    return;
                }

                _storagePath = GetAttachmentsFolderPath(SelectedDateEvent); // Гененрируем путь для хранения файла

                // Генерируем уникальное имя файла
                string originalFileName = fileInfo.Name;
                string fileExtension = fileInfo.Extension;
                string uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                string storagePath = Path.Combine(_storagePath, uniqueFileName);

                var newFile = new AttachedFileModel(_connectionString) // Создаем модель файла
                {
                    SourceFilePath = filePath, // Путь к исходному файлу
                    FileName = fileInfo.Name,
                    FilePath = storagePath,
                    FileSize = fileInfo.Length,
                    FileType = fileExtension,
                    UploadDate = DateTime.Now,
                    FileCategory = fileCategory,
                };
                newFile.MarkAsNwe(); // Устанавливаем статус файла как новый

                // Подписываемся на событие удаления файла
                newFile.FileDeleted += RemoveAttachedFile; //Подписываем событие FileDeleted на выполнение метода RemoveAttachedFile;
                // Добавляем запись в коллекцию (пока без EventId)
                if (fileCategory == FileCategory.document.ToString())
                {
                    AttachedFilesDocument.Add(newFile);
                }
                else
                {
                    AttachedFilesMonitoring.Add(newFile);
                }
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
            var selectedUnits = Units.Where(u => u.IsSelected).Select(u => u.UnitName);
            SelectedUnitsText = selectedUnits.Any()
                ? $"Выбрано: {string.Join(", ", selectedUnits)}"
                : "Ничего не выбрано";
        }

        //Сохранение новой записи 
        private async Task AddEvent(object parameters)
        {
            if (!ValidateEvent())
                return;

            var newEvent = new Event // Создание экземпляра для хранения нового Event
            {
                DateEvent = SelectedDateEvent.ToString(formatDate),
                OilRefining = Refining,
                Id_Category = _selectedCategory.Id,
                Description = Description,
                Action = Action,
                DateCreate = DateTime.Now.ToString(formatDateTime, culture), // Получить текущую дату и время
                Creator = _userName
            };
            // Сохраняем событие и получаем его ID
            _currentEventId = await _eventRepository.AddEventWithUnitsAsync(newEvent, SelectedIds); // Сохраняем событие и получаем его ID

            WriteAttachFile(AttachedFilesDocument);
            WriteAttachFile(AttachedFilesMonitoring);

            CloseWindow();
        }

        // Обновление записи события
        private void UpdatedEvent(object parameters)
        {
            if (!ValidateEvent()) return;
            // Создание экземпляра для хранения нового Event
            var _updateEvent = new Event
            {
                Id = this._editedEventId,
                DateEvent = SelectedDateEvent.ToString(AppBaseConfig.formatDate),
                OilRefining = Refining,
                Id_Category = _selectedCategory.Id,
                Description = Description,
                Action = Action
            };
            _eventRepository.UpdateEventWithUnitsAsync(_updateEvent, SelectedIds);//Обновление данных по событию и обьектам в БД 

            CommitAttachmentChanges(AttachedFilesDocument);
            CommitAttachmentChanges(AttachedFilesMonitoring);

            CloseWindow();
        }

        private void WriteAttachFile(ObservableCollection<AttachedFileModel> attachedFiles)
        {
            if (attachedFiles.Any()) // Проверяем наличие записей о файлах
            {
                // Удаляем записи с не-новым статусом
                var hasValidFiles = RemoveNonNewFiles(attachedFiles);

                if (hasValidFiles)
                {                    
                    CopyFileToPath(attachedFiles); // Копируем в папку закрепленные файлы
                    
                    if (_currentEventId.HasValue)   // Если событие есть, то записваем информацию о рикрепелнных к нему файлов в БД
                    {
                        SaveAttachedFilesToDatabase(_currentEventId.Value, attachedFiles);
                    }
                }
            }
        }

        // Отдельный метод для очистки коллекции
        private bool RemoveNonNewFiles(ObservableCollection<AttachedFileModel> attachedFiles)
        {
            bool hasNewFiles = false;

            for (int i = attachedFiles.Count - 1; i >= 0; i--)
            {
                if (attachedFiles[i].Status == FileStatus.New)
                {
                    hasNewFiles = true; // Нашли хотя бы один новый файл
                }
                else
                {
                    attachedFiles.RemoveAt(i);
                }
            }
            return hasNewFiles;
        }

        //Копирование файла 
        //private void CopyFileToPath(ObservableCollection<AttachedFileModel> attachedFile)
        //{
        //    var failedFiles = new List<AttachedFileModel>();

        //    foreach (var file in attachedFile.ToList())
        //    {
        //        if (file.Status != FileStatus.New) continue; // если файл в коллекции не является новым , то пропускаем его.

        //        try
        //        {
        //            Directory.CreateDirectory(Path.GetDirectoryName(file.FilePath));
        //            File.Copy(file.SourceFilePath, file.FilePath, overwrite: true); // overwrite на случай повторной попытки
        //        }
        //        catch (Exception ex)
        //        {
        //            failedFiles.Add(file); //Если файл не удалось скопирорвать, то помечаем его для дальнейшего исключения из записи о нем в БД
        //            MessageBox.Show($"Не удалось сохранить {file.FileName}: {ex.Message}");
        //        }
        //    }

        //    // Удаляем проблемные файлы из коллекции
        //    foreach (var badFile in failedFiles)
        //    {
        //        attachedFile.Remove(badFile);
        //    }
        //}

        //private void CopyFilesToPath(List<AttachedFileModel> files)
        //{

        //    var failedFiles = new List<AttachedFileModel>();

        //    foreach (var file in files.ToList())
        //    {
        //        try
        //        {
        //            Directory.CreateDirectory(Path.GetDirectoryName(file.FilePath));
        //            File.Copy(file.SourceFilePath, file.FilePath, overwrite: true); // overwrite на случай повторной попытки
        //        }
        //        catch (Exception ex)
        //        {
        //            failedFiles.Add(file); //Если файл не удалось скопирорвать, то помечаем его для дальнейшего исключения из записи о нем в БД
        //            MessageBox.Show($"Не удалось сохранить {file.FileName}: {ex.Message}");
        //        }
        //    }

        //    // Удаляем проблемные файлы из коллекции
        //    foreach (var badFile in failedFiles)
        //    {
        //        files.Remove(badFile);
        //    }
        //}



        //фиксации изменений файлов
        private void CommitAttachmentChanges(ObservableCollection<AttachedFileModel> attachedFiles)
        {
            if (attachedFiles.Any()) // Проверяем наличие записей о файлах
            {
                // Разделяем файлы на группы
                var (filesToSave, filesToDelete) = SeparateAttachments(attachedFiles);

                // Обрабатываем файлы для сохранения
                if (filesToSave.Any())
                {
                    SaveAttachments(filesToSave);
                }

                // Обрабатываем файлы для удаления
                if (filesToDelete.Any())
                {
                    DeleteAttachments(filesToDelete);
                }
            }
        }

        /// <summary>
        /// Разделение записей на группы новых и удаляемых файлов
        /// </summary>
        /// <param name="attachedFile">Коллекция содержащая файлы</param>
        /// <returns>Возвращает две коллекции типа List одна содержит файлы дл сохранения, другая файлы на удаление</returns>
        private (List<AttachedFileModel>, List<AttachedFileModel>) SeparateAttachments(ObservableCollection<AttachedFileModel> attachedFile)
        {
            var filesToSave = new List<AttachedFileModel>(); //Файлы которые надо сохранить 
            var filesToDelete = new List<AttachedFileModel>(); //Файлы которые надо удалить

            foreach (var file in attachedFile)
            {
                if (file.Status == FileStatus.New)
                {
                    filesToSave.Add(file);
                }
                else if (file.Status == FileStatus.Deleted)
                {
                    filesToDelete.Add(file);
                }
            }
            return (filesToSave, filesToDelete);
        }

        private void SaveAttachments(List<AttachedFileModel> filesToSave)
        {
            CopyFilesToPath(filesToSave); // Копируем только новые файлы
            SaveAttachedFilesToDatabase(_editedEventId, filesToSave);
        }

        private void DeleteAttachments(List<AttachedFileModel> filesToDelete)
        {
            foreach (var file in filesToDelete)
            {
                if (file.FileId > 0) // Если файл уже был в БД
                {
                    DeleteFileFromDatabase(file.FileId);
                }
                DeleteFileFromDisk(file.FilePath);
            }
        }

        // Удаляем запись из БД 
        private void DeleteFileFromDatabase(int fileId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var cmd = new SQLiteCommand("DELETE FROM AttachedFiles WHERE FileId = @id", connection);
                cmd.Parameters.AddWithValue("@id", fileId);
                cmd.ExecuteNonQuery();
            }
        }


        // Вспомогательные методы

        private void DeleteFileFromDisk(string path)
        {
            try
            {
                // 1. Удаление файла с диска
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить файл: {ex.Message}");
            }
        }


        //Валидация перед сохранением:
        private bool ValidateEvent()
        {
            if (SelectedIds == null || !SelectedIds.Any()) // Проверка выбранной установки
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


        private void SaveAttachedFilesToDatabase(long eventId, ObservableCollection<AttachedFileModel> attachedFiles)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                foreach (var file in attachedFiles)
                {
                    string query = @" INSERT INTO AttachedFiles (EventId, FileCategory, FileName, FilePath, FileSize, FileType, UploadDate)" +
                                    " VALUES (@EventId, @FileCategory, @FileName, @FilePath, @FileSize, @FileType, @UploadDate)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EventId", eventId);
                        command.Parameters.AddWithValue("@FileCategory", file.FileCategory);
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

        private void SaveAttachedFilesToDatabase(long eventId, List<AttachedFileModel> files)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                foreach (var file in files)
                {
                    string query = @" INSERT INTO AttachedFiles (EventId, FileCategory, FileName, FilePath, FileSize, FileType, UploadDate)" +
                                    " VALUES (@EventId, @FileCategory, @FileName, @FilePath, @FileSize, @FileType, @UploadDate)";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EventId", eventId);
                        command.Parameters.AddWithValue("@FileCategory", file.FileCategory);
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
            AttachedFilesDocument.Clear();

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
                                FileCategory = reader.GetString(2),
                                FileName = reader.GetString(3),
                                FilePath = reader.GetString(4),
                                FileSize = reader.GetInt64(5),
                                UploadDate = DateTime.Parse(reader.GetString(6)),
                                FileType = reader.GetString(7)
                            };
                            loadFile.MarkAsExisting(); // Маркируем файл как существующий
                            // Подписываемся на событие удаления файла
                            loadFile.FileDeleted += RemoveAttachedFile;
                            if (loadFile.FileCategory == FileCategory.document.ToString())
                            {
                                AttachedFilesDocument.Add(loadFile); // Добавляем файл в коллекцию
                            }
                            else
                            {
                                AttachedFilesMonitoring.Add(loadFile); // Добавляем файл в коллекцию
                            }
                        }
                    }
                }
            }
        }

        public bool CanSave //свойство, которое проверяет все обязательные поля:
        {
            get
            {
                return SelectedIds != null && SelectedIds.Any() &&
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
            AttachedFilesDocument.Clear(); // Очищаем коллекцию прикрепленных файлов

            // Закрытие окна, в котором находится этот ViewModel
            Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?
                .Close();
        }

        private void DeleteAttachedFile(AttachedFileModel file)
        {
            if (file != null) return;
            // Удаляем файл из коллекции
            AttachedFilesDocument.Remove(file);
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
