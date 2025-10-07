using FlowEvents.Models;
using FlowEvents.Models.Enums;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
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

        private string _storagePath;                // = "C:\\temp\\Attachments";  // Путь для хранения файлов

        private long _currentEventId;              // Будет установлен после сохранения события
        private long _editedEventId;                 // Идентификатор редактируемого события, если есть

        private bool _isLoading;

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

        public string SelectedUnitsText // Текст выбраных объектов
        {
            get => _selectedUnitsText;
            set
            {
                _selectedUnitsText = value;
                OnPropertyChanged(nameof(SelectedUnitsText));
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
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

        // --- Репозитории ---
        private readonly IUnitRepository _unitRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IEventUnitRepository _eventUnitRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IAttachFilesRepository _attachFilesRepository;

        // --- Сервисы ---
        private readonly IFileService _fileService;


        // Конструктор ViewModel для НОВЫХ события
        public EventViewModel(IUnitRepository unitRepository,
                                ICategoryRepository categoryRepository,
                                IEventRepository eventRepository,
                                IFileService fileService)
        {
            // Инициализация репозиториев
            _unitRepository = unitRepository;
            _categoryRepository = categoryRepository;
            _eventRepository = eventRepository;

            // Инициализация сервиса файлов
            _fileService = fileService;

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
        public EventViewModel(EventForView eventToEdit,
                                IUnitRepository unitRepository,
                                ICategoryRepository categoryRepository,
                                IEventUnitRepository eventUnitRepository,
                                IEventRepository eventRepository,
                                IFileService fileService,
                                IAttachFilesRepository attachFilesRepository)
        {
            // Инициализация репозиториев
            _unitRepository = unitRepository;
            _categoryRepository = categoryRepository;
            _eventUnitRepository = eventUnitRepository;
            _eventRepository = eventRepository;
            _attachFilesRepository = attachFilesRepository;


            // Инициализация сервиса файлов
            _fileService = fileService;

            _originalEvent = eventToEdit;

            AttachFileCommand = new RelayCommand(AttachFile);
            SaveCommand = new RelayCommand(UpdatedEvent);
            CancelCommand = new RelayCommand(Cancel);

            _editedEventId = _originalEvent.Id; // Сохраняем ID редактируемого события
            SelectedDateEvent = _originalEvent.DateEvent; // Установка даты события
            Refining = _originalEvent.OilRefining;
            Description = _originalEvent.Description;
            Action = _originalEvent.Action;

            // Запускаем инициализацию без блокировки конструктора
            _ = InitializeForEditAsync();

            SubscribeToUnitsPropertyChanged(); // Подписка на изменение IsSelected (чтобы Label обновлялся автоматически при выборе или снятии чекбокса установки)
            UpdateSelectedUnitsText(); // Инициализация текстового перечня выделенных объектов для события
        }

        private async Task InitializeForEditAsync()
        {
            await InitializeAsync();
            SelectedCategory = Categories.FirstOrDefault(c => c.Name == _originalEvent.Category);
            
            await LoadSelectedUnitsForEvent(_editedEventId); // Получение перечень установок связанных с этим событием
            await LoadAttachedFiles(_editedEventId); // Получаем перечень прикрепленных файлов.
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
        public async Task LoadSelectedUnitsForEvent(long eventId)
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

        private void AttachFile(object parameter)
        {
            var fileCategory = parameter as string;

            string filePath = SelectFile(); // Вызываем диалоговое окно для выбора файла
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var fileInfo = _fileService.GetFileInfo(filePath);

                // Порог размера файла в МБ устанавливается в настройках приложения
                if (fileInfo.Length > App.Settings.SizeFileAttachment * 1024 * 1024)
                {
                    MessageBox.Show($"Файл слишком большой. Максимальный размер: {App.Settings.SizeFileAttachment} МБ.");
                    return;
                }

                _storagePath = _fileService.GenerateAttachmentsPath(SelectedDateEvent);  // Гененрируем путь для хранения файла

                // Генерируем уникальное имя файла
                string originalFileName = fileInfo.Name;
                string fileExtension = fileInfo.Extension;
                string uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                string storagePath = Path.Combine(_storagePath, uniqueFileName);

                var newFile = new AttachedFileModel() // Создаем модель файла
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

        private void UpdateSelectedUnitsText() // Формирование перечня выбранных установок
        {
            var selectedUnits = Units.Where(u => u.IsSelected).Select(u => u.UnitName);
            SelectedUnitsText = selectedUnits.Any()
                ? $"Выбрано: {string.Join(", ", selectedUnits)}"
                : "Ничего не выбрано";
        }

        //Сохранение новой записи 
        private async Task AddEvent(object parameters)
        {
            if (!ValidateEvent())   //Проверка на обязательные пункты в заполнении о событии
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

            await CommitAttachmentChanges(_currentEventId, AttachedFilesDocument);
            await CommitAttachmentChanges(_currentEventId, AttachedFilesMonitoring);

            CloseWindow();
        }

        // Обновление записи события
        private async Task UpdatedEvent(object parameters)
        {
            if (!ValidateEvent())    //Проверка на обязательные пункты в заполнении о событии
                return;
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
            await _eventRepository.UpdateEventWithUnitsAsync(_updateEvent, SelectedIds);//Обновление данных по событию и обьектам в БД 

            await CommitAttachmentChanges(_editedEventId, AttachedFilesDocument);
            await CommitAttachmentChanges(_editedEventId, AttachedFilesMonitoring);

            CloseWindow();
        }



        //Используется для сохранения и удаления прикрепленных файлов при редактировании события 
        private async Task CommitAttachmentChanges(long eventID, ObservableCollection<AttachedFileModel> attachedFiles)
        {
            if (attachedFiles.Any()) // Проверяем наличие записей о файлах
            {
                // Разделяем файлы на группы
                var (filesToSave, filesToDelete) = SeparateAttachments(attachedFiles);

                // Обрабатываем файлы для сохранения
                if (filesToSave.Any())
                {
                    await CopyFilesToPath(filesToSave); // Копируем только новые файлы
                    await _fileService.SaveAttachedFilesToDatabase(eventID, filesToSave); // Сохраняем информацию о файлах в БД
                }

                // Обрабатываем файлы для удаления
                if (filesToDelete.Any())
                {
                    foreach (var file in filesToDelete)
                    {
                        if (file.FileId > 0) // Если файл уже был в БД
                        {
                            await _fileService.DeleteFileWithConfirmation(file.FileId, file.FilePath);
                        }
                    }
                }
            }
        }


        // Копирование файлов 
        private async Task CopyFilesToPath(List<AttachedFileModel> files) // Копирование файлов на диск
        {

            var failedFiles = new List<AttachedFileModel>(); // Список для хранения файлов, которые не удалось скопировать

            foreach (var file in files.ToList()) // Проходим по копии списка, чтобы избежать проблем с изменением коллекции во время итерации
            {
                try
                {
                    bool success = await _fileService.CopyFileAsync(file.SourceFilePath, file.FilePath);
                    if (!success)
                    {
                        failedFiles.Add(file); //Если файл не удалось скопирорвать, то помечаем его для дальнейшего исключения из записи о нем в БД
                    }
                }
                catch (Exception ex)
                {
                    failedFiles.Add(file); //Если файл не удалось скопирорвать, то помечаем его для дальнейшего исключения из записи о нем в БД
                    MessageBox.Show($"Не удалось сохранить {file.FileName}: {ex.Message}");
                }
            }
            foreach (var badFile in failedFiles)    // Удаляем проблемные файлы из коллекции
            {
                files.Remove(badFile);
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


        private async Task LoadAttachedFiles(long eventId)
        {
            try
            {
                AttachedFilesDocument.Clear();
                AttachedFilesMonitoring.Clear();

                var files = await _attachFilesRepository.GetByEventIdAsync(eventId);

                foreach (var file in files)
                {
                    file.MarkAsExisting();
                    file.FileDeleted += RemoveAttachedFile;

                    if (file.FileCategory == FileCategory.document.ToString())
                    {
                        AttachedFilesDocument.Add(file);
                    }
                    else
                    {
                        AttachedFilesMonitoring.Add(file);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить прикрепленные файлы");
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
