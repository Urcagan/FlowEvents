using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Windows;

namespace FlowEvents
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public AppSettings appSettings; // Объект параметров приложения
        public string _connectionString; // = $"Data Source={Global_Var.pathDB};Version=3;foreign keys=true;";

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        private DateTime _sartDate = DateTime.Now; // Значение по умолчанию
        public DateTime StartDate
        {
            get => _sartDate;
            set
            {
                if (value.Date <= EndDate.Date)
                {
                    _sartDate = value;
                    OnPropertyChanged(nameof(StartDate));
                    UpdateQuery();
                }
            }
        }

        private DateTime _endDate = DateTime.Now; // Значение по умолчанию
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (value.Date >= StartDate.Date)
                {
                    _endDate = value;
                    OnPropertyChanged(nameof(EndDate));
                    UpdateQuery();
                }
            }
        }

        private UnitModel _selectedUnit;
        public UnitModel SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged();
                UpdateQuery();
            }
        }

        private bool _isAllEvents;
        public bool IsAllEvents
        {
            get => _isAllEvents;
            set
            {
                _isAllEvents = value;
                OnPropertyChanged();
                UpdateQuery();
            }
        }

        private string _queryEvent; // Для хранения Запроса получения Events 
        public string QueryEvent
        {
            get => _queryEvent;
            set
            {
                _queryEvent = value;
                LoadEvents();
            }
        }

        private void UpdateQuery()
        {
            QueryEvent = BuildSQLQueryEvents(SelectedUnit, IsAllEvents ? null : (DateTime?)StartDate, IsAllEvents ? null : (DateTime?)EndDate, IsAllEvents);
        }

        public ObservableCollection<UnitModel> Units { get; set; } = new ObservableCollection<UnitModel>();

        // Коллекция для записей журнала (автоматически уведомляет об изменениях)
        public ObservableCollection<EventsModelForView> Events { get; set; } = new ObservableCollection<EventsModelForView>();

        private EventsModelForView _selectedEvent; // Выбранное событие в таблице
        public EventsModelForView SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                if (_selectedEvent == value)
                    return; // Не обновлять, если значение не изменилось
                Console.WriteLine($"SelectedEvent changed from {_selectedEvent?.Id} to {value?.Id}");
                _selectedEvent = value;
                OnPropertyChanged(nameof(SelectedEvent));
                OnPropertyChanged(nameof(SelectedEventCollection)); // Критически важно!
                OnPropertyChanged(nameof(MonitoringFiles)); //Фильтрация привязанных файлов по признаку мониторига
                OnPropertyChanged(nameof(DocumentFiles));
            }
        }

        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand SettingOpenWindow { get; }
        public RelayCommand UnitOpenWindow { get; }
        public RelayCommand CategoryOpenWindow { get; }
        public RelayCommand EventAddWindow { get; }
        public RelayCommand EditEventCommand { get; } // команда для редактирования
        public RelayCommand DeleteEventCommand { get; }
        public RelayCommand UserManagerWindow { get; }
        public RelayCommand DownDateCommand { get; }
        public RelayCommand UpDateCommand { get; }
        public RelayCommand CheckUpdateAppCommand { get; } // Кнопка проверки обновления программы 


        private readonly IEventRepository _eventRepository;

        //===============================================================================================================================================

        public MainViewModel()
        {
            SettingOpenWindow = new RelayCommand(SettingsMenuItem);
            UnitOpenWindow = new RelayCommand(UnitMenuItem);
            CategoryOpenWindow = new RelayCommand(CategoryMenuItem);
            EventAddWindow = new RelayCommand(EventAddBtb);
            EditEventCommand = new RelayCommand(EditEvent);
            DeleteEventCommand = new RelayCommand(DeletEvent);
            UserManagerWindow = new RelayCommand(UserManagerMenuItem);
            CheckUpdateAppCommand = new RelayCommand(CheckUpdateApp);

            DownDateCommand = new RelayCommand((parameter) =>
            {
                StartDate = StartDate.AddDays(-1);
                EndDate = EndDate.AddDays(-1);
            });
            UpDateCommand = new RelayCommand((parameter) =>
            {
                EndDate = EndDate.AddDays(1);
                StartDate = StartDate.AddDays(1);

            });

            User();
            //    appSettings = AppSettings.GetSettingsApp(); // Загружаем настройки программы из файла при запуске программы

        }

        // 
        public IEnumerable<AttachedFileModel> MonitoringFiles =>
            SelectedEvent?.AttachedFiles?
                .Where(f => f.FileCategory?.Contains("monitoring") == true)
        ?? Enumerable.Empty<AttachedFileModel>();

        public IEnumerable<AttachedFileModel> DocumentFiles =>
            SelectedEvent?.AttachedFiles?
                .Where(f => f.FileCategory?.Contains("document") == true)
        ?? Enumerable.Empty<AttachedFileModel>();

        // 
        public ObservableCollection<EventsModelForView> SelectedEventCollection =>
            SelectedEvent == null
                ? new ObservableCollection<EventsModelForView>()
                : new ObservableCollection<EventsModelForView> { SelectedEvent };

        public void StartUP()
        {
            //         appSettings = AppSettings.GetSettingsApp(); // Загружаем настройки программы из файла при запуске программы

            string pathDB = App.Settings.pathDB; //appSettings.pathDB;
            string verDB = App.Settings.VerDB; //appSettings.VerDB;

            if (!CheckDB.CheckPathToFileDB(pathDB)) return;   // Проверяем путь к базе данных и выходим, если он неверен

            _connectionString = $"Data Source={pathDB};Version=3;foreign keys=true;"; //Формируем строку подключения к БД

            // Проверка версии базы данных
            if (!CheckDB.IsDatabaseVersionCorrect(verDB, _connectionString))  //проверка версии базы данных
            {
                MessageBox.Show($"Версия БД не соответствует требуемой версии {verDB}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadUnitsToComboBox(); // Загружаем перечень установок из базы данных

            FilePath = pathDB; //Выводим путь к файлу в нижную часть главного окна            
        }

        public void LoadUnitsToComboBox()
        {
            Units.Clear();
            Units.Insert(0, new UnitModel { Id = -1, Unit = "Все" });
            // Загружаем перечень установок из базы данных
            GetUnitFromDatabase();
            SelectedUnit = Units.FirstOrDefault();
        }

        // ------------------------------------------------------------------------------------------------------
        public void CheckRoleUser()
        {
            // Проверяем роль пользователя и устанавливаем видимость кнопок
            //        if (appSettings.UserRole == 1) // Администратор
            //        {
            IsCategoryButtonVisible = true;
            IsUnitButtonVisible = true;
            IsToolBarVisible = true;
            //}
            //else if (appSettings.UserRole == 2) // Оператор
            //{
            //    IsCategoryButtonVisible = false;
            //    IsUnitButtonVisible = false;
            //    IsToolBarVisible = true;
            //}
            //else // Гость
            //{
            //    IsCategoryButtonVisible = false;
            //    IsUnitButtonVisible = false;
            //    IsToolBarVisible = false;
            //}
        }

        // Метод для загрузки данных из базы
        public void LoadEvents()
        {
            CheckRoleUser(); // Проверяем роль пользователя и устанавливаем видимость кнопок

            Events.Clear();

            // Получаем данные из базы
            if (QueryEvent == null) return;
            var eventsFromDb = GetEvents(QueryEvent);

            // Добавляем данные в коллекцию
            foreach (var eventModel in eventsFromDb)
            {
                Events.Add(eventModel);
            }
        }

        public void UpdateConnectionString(string newPathDB)
        {
            _connectionString = $"Data Source={newPathDB};Version=3;foreign keys=true;";
        }




        private void UnitMenuItem(object parameter)
        {
            var unitViewModel = new UnitViewModel(this);
            UnitsView unitsView = new UnitsView(unitViewModel);
            unitsView.Closed += UnitsView_Closed; // Подписываемся на событие Closed окна UnitsView

            if (unitsView.ShowDialog() == true) { }
        }

        private void UnitsView_Closed(object sender, EventArgs e)
        {
            LoadUnitsToComboBox(); // Перезагружаем установки после закрытия окна UnitsView

        }

        private void CategoryMenuItem(object parameter)
        {
            var categoryViewModel = new CategoryViewModel(this);
            CategoryView categoryView = new CategoryView(categoryViewModel);
            categoryView.Closed += CategoryView_Closed;
            if (categoryView.ShowDialog() == true) { }
        }

        private void CategoryView_Closed(object sender, EventArgs e)
        {
            //LoadEvents();
        }

        private void SettingsMenuItem(object parameter)
        {
            // Создаем и показываем окно настроек
            SettingsWindow settingsWindow = new SettingsWindow(this);
            if (settingsWindow.ShowDialog() == true) { }
        }

        private void UserManagerMenuItem(object parameter)
        {
            var userManagerModel = new UserManagerModel(this);
            // Создаем и показываем окно пользователей
            UserManager userManager = new UserManager(userManagerModel);
            if (userManager.ShowDialog() == true) { }
        }


        private void EventAddBtb(object parameter)
        {
            var eventViewModel = new EventViewModel(this);
            EventWindow eventView = new EventWindow(eventViewModel);

            eventView.Closed += EventAdd_Closed;
            if (eventView.ShowDialog() == true) { }
        }

        private void EventAdd_Closed(object sender, EventArgs e)
        {
            LoadEvents();
        }


        private void EditEvent(object parameter)
        {
            if (parameter is EventsModelForView selectedEvent)
            {
                // Создаем ViewModel для редактирования, передавая выбранное событие
                var eventViewModel = new EventViewModel(this, selectedEvent);

                // Создаем и показываем окно редактирования
                var eventWindow = new EventWindow(eventViewModel);

                eventWindow.Closed += EventAdd_Closed;
                eventWindow.ShowDialog();
            }
        }


        private void ExecuteDeleteCommand(SQLiteConnection connection, string query, int eventId)
        {
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventId", eventId);
                command.ExecuteNonQuery();
            }
        }


        private void RemoveEventById(int id) // Удаление события из коллекции по ID
        {
            if (Events == null || Events.Count == 0) return;

            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].Id == id)
                {
                    Events.RemoveAt(i);
                    return; // Выходим после удаления
                }
            }
        }


        // Удаление события
        private void DeletEvent(object parameter)
        {
            List<AttachedFileForEvent> listAttachedFIles = new List<AttachedFileForEvent>();

            if (parameter is EventsModelForView selectedEvent)
            {
                int eventId = selectedEvent.Id; //Получили ID события которое надо удалить

                var confirm = MessageBox.Show(
                    $"Вы действительно хотите удалить событие от {selectedEvent.DateEvent:dd.MM.yyyy} по объекту {selectedEvent.Unit} ?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                // 1. Получаем пепречень файлов связанных с этим событием
                listAttachedFIles = GetIdFilesOnEvent(eventId);

                // 2. Удаляем Файлы связанные с событием
                DeleteFiles(listAttachedFIles);

                // 3. Удалям записи о событии из БД (таблица Events) Записи о файлах удаляются автоматически каскадом
                DeleteEventFromDB(eventId);

            }
        }

        // Удаление файлов связанных с событием
        private bool DeleteFiles(List<AttachedFileForEvent> listAttact)
        {
            if (!listAttact.Any()) return true;

            bool allDeleted = true;

            foreach (var file in listAttact)
            {
                if (!DeleteFileFromDisk(file.FilePath))
                    allDeleted = false;
            }
            return allDeleted;
        }

        private bool DeleteFileFromDisk(string path)
        {
            try
            {
                // 1. Удаление файла с диска
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                return false; // файл не существовал
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить файл: {ex.Message}");
                return false;
            }
        }


        private async void CheckForUpdate(string path)
        {
            await Updater.CheckForUpdateAsync(path);//Вызов метода для проверки обновлений
        }

        //Проверка обновления программы
        private void CheckUpdateApp(object parameter) 
        {
            MessageBox.Show("Проверка обновлений");
                        
            try
            {
                string repositoryPath = App.Settings.UpdateRepository; //Получаем из конфигурации путь к файлу с параметрами обновления приложения
                CheckForUpdate(repositoryPath); //Проверка обновлений. И выполнение обновления
            }
            catch (Exception ex)
            {
                //Обрабатываем ошибки
                MessageBox.Show("Ошибка: " + ex.Message, "Обработка ошибки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }


        //------------------------------------------------------------------------------------------------------
        //Запросы к базе данных
        //------------------------------------------------------------------------------------------------------

        //Получить перечень файлов связанных с событием
        private List<AttachedFileForEvent> GetIdFilesOnEvent(int EventId)
        {
            List<AttachedFileForEvent> attachedFile = new List<AttachedFileForEvent>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @" Select FileId, EventId, FileCategory, FileName, FilePath From AttachedFiles Where EventId = @SelectedRowId ";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SelectedRowId", EventId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                attachedFile.Add(new AttachedFileForEvent
                                {
                                    FileId = reader.GetInt32(0),
                                    EventId = reader.GetInt32(1), // ID события с которым связан файл
                                    FileName = reader.IsDBNull(2) ? null : reader.GetString(2), // Имя файла
                                    FilePath = reader.IsDBNull(3) ? null : reader.GetString(3), // Путь к файлу на диске, где он хранится
                                });
                            }
                        }
                    }
                }
                return attachedFile;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void GetUnitFromDatabase() //Загрузка перечня установок из ДБ
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


        /// <summary>
        /// Формирует строку SQL запроса для вывода данных в таблицу 
        /// </summary>
        /// <param name="selectedUnit"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public string BuildSQLQueryEvents(UnitModel selectedUnit, DateTime? startDate, DateTime? endDate, bool isAllEvents)
        {
            var conditions = new List<string>();

            // Фильтрация по установке
            if (selectedUnit != null && selectedUnit.Id != -1)
            {
                string unitName = selectedUnit.Unit.Replace("'", "''");
                conditions.Add($"Unit LIKE '%{unitName}%'");
            }

            // Фильтрация по времени (если не выбрано "Все даты")
            if (!isAllEvents)
            {
                string startDateStr = startDate?.Date.ToString("yyyy-MM-dd") ?? DateTime.MinValue.Date.ToString("yyyy-MM-dd");
                string endDateStr = endDate?.Date.ToString("yyyy-MM-dd") ?? DateTime.MaxValue.Date.ToString("yyyy-MM-dd");
                conditions.Add($"DateEvent BETWEEN '{startDateStr}' AND '{endDateStr}'");
            }

            // Основной запрос теперь включает LEFT JOIN к AttachedFiles
            string query = @"SELECT e.id, e.DateEvent, e.Unit, e.OilRefining, e.Category, e.Description, e.Action, e.DateCreate, e.Creator, 
                            af.FileId, af.FileCategory, af.FileName, af.FilePath, af.FileSize, af.FileType, af.UploadDate
                            FROM vwEvents e LEFT JOIN AttachedFiles af ON e.id = af.EventId";

            // Добавляем условия, если они есть
            if (conditions.Count > 0)
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }

            return query;
        }


        // Метод для получения событий из базы данных
        public List<EventsModelForView> GetEvents(string queryEvent)
        {
            var eventsDict = new Dictionary<int, EventsModelForView>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(queryEvent, connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int eventId = reader.GetInt32(reader.GetOrdinal("id"));

                        // Если события еще нет в словаре, добавляем его
                        if (!eventsDict.TryGetValue(eventId, out var eventModel))
                        {
                            eventModel = new EventsModelForView
                            {
                                Id = eventId,
                                DateEventString = reader.GetString(reader.GetOrdinal("DateEvent")),
                                Unit = reader.GetString(reader.GetOrdinal("Unit")),
                                OilRefining = reader.IsDBNull(reader.GetOrdinal("OilRefining")) ? null : reader.GetString(reader.GetOrdinal("OilRefining")),
                                Category = reader.GetString(reader.GetOrdinal("Category")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                Action = reader.IsDBNull(reader.GetOrdinal("Action")) ? null : reader.GetString(reader.GetOrdinal("Action")),
                                DateCreate = reader.GetString(reader.GetOrdinal("DateCreate")),
                                Creator = reader.GetString(reader.GetOrdinal("Creator"))
                            };
                            eventsDict[eventId] = eventModel;
                        }

                        // Если есть прикрепленный файл, добавляем его
                        if (!reader.IsDBNull(reader.GetOrdinal("FileId")))
                        {
                            eventModel.AttachedFiles.Add(new AttachedFileModel(_connectionString) // Передаем строку подключения
                            {
                                FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
                                FileCategory = reader.GetString(reader.GetOrdinal("FileCategory")),
                                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                                FileType = reader.GetString(reader.GetOrdinal("FileType")),
                                UploadDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("UploadDate")))
                            });
                        }
                    }
                }
            }

            return eventsDict.Values.ToList();
        }



        private void DeleteEventFromDB(int eventId)
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
                            ExecuteDeleteCommand(connection, "DELETE FROM Events WHERE ID = @EventId", eventId);

                            // ExecuteDeleteCommand(connection, "DELETE FROM EventUnits WHERE EventID = @EventId", eventId);

                            transaction.Commit();

                            RemoveEventById(eventId); // Удаляем из UI только после успешной транзакции

                        }
                        catch (Exception ex)
                        {
                            // В случае ошибки откатить транзакцию
                            transaction.Rollback();
                            MessageBox.Show($"Не удалось удалении событие {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}");
            }

        }



        //====================================================================================================
        //Свойство для управления видимостью кнопок

        //Кнопка Создать
        private bool _isCategoryButtonVisible;
        public bool IsCategoryButtonVisible
        {
            get => _isCategoryButtonVisible;
            set
            {
                _isCategoryButtonVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _isUnitButtonVisible;
        public bool IsUnitButtonVisible
        {
            get => _isUnitButtonVisible;
            set
            {
                _isUnitButtonVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _isToolBarVisible;

        public bool IsToolBarVisible
        {
            get { return _isToolBarVisible; }
            set
            {
                _isToolBarVisible = value;
                OnPropertyChanged();
            }
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для генерации события
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void User()
        {
            var identity = WindowsIdentity.GetCurrent();

            // Простые свойства
            UserName = Environment.UserName;
            string Domain = Environment.UserDomainName;

            // Более детальная информация
            string FullName = identity.Name;
            string AuthType = identity.AuthenticationType;
            string IsAuthenticated = identity.IsAuthenticated.ToString();
            string IsSystem = identity.IsSystem.ToString();
            string IsGuest = identity.IsGuest.ToString();
        }

        

    }

}
