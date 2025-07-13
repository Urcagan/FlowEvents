using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

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

        public RelayCommand SettingOpenWindow { get; }
        public RelayCommand UnitOpenWindow { get; }
        public RelayCommand CategoryOpenWindow { get; }
        public RelayCommand EventAddWindow { get; }
        public RelayCommand EditEventCommand { get; } // команда для редактирования
        public RelayCommand DeleteEventCommand { get; }
        public RelayCommand UserManagerWindow { get; }
        public RelayCommand DownDateCommand { get; }
        public RelayCommand UpDateCommand { get; }

        //===============================================================================================================================================

        public MainViewModel()
        {
            SettingOpenWindow = new RelayCommand(SettingsMenuItem);
            UnitOpenWindow = new RelayCommand(UnitMenuItem);
            CategoryOpenWindow = new RelayCommand(CategoryMenuItem);
            EventAddWindow = new RelayCommand(EventAddBtb);
            EditEventCommand = new RelayCommand(EditEvent);
            DeleteEventCommand = new RelayCommand(Delete);
            UserManagerWindow = new RelayCommand(UserManagerMenuItem);
            
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

            appSettings = AppSettings.GetSettingsApp(); // Загружаем настройки программы из файла при запуске программы
        }

        public void StartUP()
        {
            //         appSettings = AppSettings.GetSettingsApp(); // Загружаем настройки программы из файла при запуске программы

            string pathDB = appSettings.pathDB;
            string verDB = appSettings.VerDB;

            if (!CheckDB.CheckPathToFileDB(pathDB)) return;   // Проверяем путь к базе данных и выходим, если он неверен

            _connectionString = $"Data Source={pathDB};Version=3;foreign keys=true;"; //Формируем строку подключения к БД

            // Проверка версии базы данных
            if (!CheckDB.IsDatabaseVersionCorrect(verDB, _connectionString))  //проверка версии базы данных
            {
                MessageBox.Show($"Версия БД не соответствует требуемой версии {verDB}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //if (!CheckDB.CheckDatabaseFileVer(pathDB, appSettings.VerDB)) return; //Проверяем версию БД

            LoadUnitsToComboBox(); // Загружаем перечень установок из базы данных
            //Units.Insert(0, new UnitModel { Id = -1, Unit = "Все" });
            //GetUnitFromDatabase();
            //SelectedUnit = Units.FirstOrDefault();

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

        // Метод для загрузки данных из базы
        public void LoadEvents()
        {
            IsCategoryButtonVisible = true;
            IsUnitButtonVisible = true;
            IsToolBarVisible = true;

            Events.Clear();

            // Получаем данные из базы
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
            //LoadEvents();
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
            var eventAddViewModel = new EventAddViewModel(this);
            EventAddWindow eventView = new EventAddWindow(eventAddViewModel);
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
                var eventEditViewModel = new EventEditViewModel(this, selectedEvent);

                // Создаем и показываем окно редактирования
                var eventEditWindow = new EventEditWindow(eventEditViewModel);
                eventEditWindow.Closed += EventAdd_Closed;
                eventEditWindow.ShowDialog();
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


        private void datePicker_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            //DateTime? selectedDate = calendar;
            MessageBox.Show("DatePicker");
        }

        //------------------------------------------------------------------------------------------------------
        //Запросы к базе данных
        //------------------------------------------------------------------------------------------------------

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

            // Основной запрос
            string query = "SELECT id, DateEvent, Unit, OilRefining, Category, Description, Action, DateCreate, Creator FROM vwEvents";

            // Добавляем условия, если они есть
            if (conditions.Count > 0)
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }

            return query;
        }
        
        //public string buildSQLQueryEvents(UnitModel selectedUnit, DateTime startDate, DateTime endDate)
        //{
        //    string query;
        //    if (selectedUnit == null || selectedUnit.Id == -1)
        //    {
        //        // Если выбрана установка "Все", то возвращаем все события за указанный период
        //        query = $" SELECT id, DateEvent, Unit, OilRefining, Category, Description, Action, DateCreate, Creator FROM vwEvents " +
        //           $"Where DateEvent BETWEEN '{startDate.Date.ToString("yyyy-MM-dd")}' AND '{endDate.Date.ToString("yyyy-MM-dd")}' ";
        //        return query;
        //    }
        //    else
        //    {
        //        string unitName = selectedUnit.Unit.Replace("'", "''"); // Экранируем одинарные кавычки в названии установки
        //        // Формируем SQL запрос для получения данных по выбранной установке
        //        query = $" SELECT id, DateEvent, Unit, OilRefining, Category, Description, Action, DateCreate, Creator FROM vwEvents " +
        //           $"Where Unit Like '%{unitName}%' AND DateEvent BETWEEN '{startDate.Date.ToString("yyyy-MM-dd")}' AND '{endDate.Date.ToString("yyyy-MM-dd")}' ";
        //        return query;
        //    }
        //}
        
        // Загрузка категорий из базы данных
        public List<EventsModelForView> GetEvents(string queryEvent)
        {
            var events = new List<EventsModelForView>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                //string query = $" SELECT id, DateEvent, Unit, OilRefining, Category, Description, Action, DateCreate, Creator FROM vwEvents " +
                //    $"Where DateEvent BETWEEN '{StartDate.Date.ToString("yyyy-MM-dd")}' AND '{EndDate.Date.ToString("yyyy-MM-dd")}' ";

                var command = new SQLiteCommand(QueryEvent, connection);
                using (var reader = command.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("id");
                    int dateIndex = reader.GetOrdinal("DateEvent");
                    int unitIndex = reader.GetOrdinal("Unit");
                    int refiningIndex = reader.GetOrdinal("OilRefining");
                    int categotyIndex = reader.GetOrdinal("Category");
                    int descriptionIndex = reader.GetOrdinal("Description");
                    int actionIndex = reader.GetOrdinal("Action");
                    int createIndex = reader.GetOrdinal("DateCreate");
                    int creatorIndex = reader.GetOrdinal("Creator");

                    while (reader.Read())
                    {
                        events.Add(new EventsModelForView
                        {
                            Id = reader.GetInt32(idIndex),
                            DateEventString = reader.GetString(dateIndex),
                            Unit = reader.GetString(unitIndex),
                            OilRefining = reader.IsDBNull(refiningIndex) ? null : reader.GetString(refiningIndex),
                            Category = reader.GetString(categotyIndex),
                            Description = reader.IsDBNull(descriptionIndex) ? null : reader.GetString(descriptionIndex),
                            Action = reader.IsDBNull(actionIndex) ? null : reader.GetString(actionIndex),
                            DateCreate = reader.GetString(createIndex),
                            Creator = reader.GetString(creatorIndex)
                        });
                    }
                }
                return events;
            }
        }

        private void Delete(object parameter)
        {
            if (parameter is EventsModelForView selectedEvent)
            {
                int eventId = selectedEvent.Id;

                var confirm = MessageBox.Show(
                    $"Вы действительно хотите удалить событие от {selectedEvent.DateEvent:dd.MM.yyyy} по объекту {selectedEvent.Unit} ?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

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
    }
}
