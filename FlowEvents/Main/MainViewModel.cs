﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Data.SQLite;
using System.Windows.Documents;

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


        // Коллекция для хранения данных (автоматически уведомляет об изменениях)
        public ObservableCollection<EventsModelForView> Events { get; set; } = new ObservableCollection<EventsModelForView>();

        public RelayCommand SettingOpenWindow { get; }
        public RelayCommand UnitOpenWindow { get; }
        public RelayCommand CategoryOpenWindow { get; }
        public RelayCommand EventAddWindow { get; }
        public RelayCommand EditEventCommand { get; } // команда для редактирования
        public RelayCommand DeleteEventCommand { get; }


        //===============================================================================================================================================

        public MainViewModel()
        {
            SettingOpenWindow = new RelayCommand(SettingsMenuItem);
            UnitOpenWindow = new RelayCommand(UnitMenuItem);
            CategoryOpenWindow = new RelayCommand(CategoryMenuItem);
            EventAddWindow = new RelayCommand(EventAddBtb);
            EditEventCommand = new RelayCommand(EditEvent);
            DeleteEventCommand = new RelayCommand(Delete);

            appSettings = AppSettings.GetSettingsApp(); // Загружаем настройки программы из файла при запуске программы

            //IsCategoryButtonVisible = false;
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
                MessageBox.Show(
                    $"Версия БД не соответствует требуемой версии {verDB}", 
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }

            //if (!CheckDB.CheckDatabaseFileVer(pathDB, appSettings.VerDB)) return; //Проверяем версию БД
               
            LoadEvents();// Загрузка данных из БД

            FilePath = pathDB; //Выводим путь к файлу в нижную часть главного окна
        }


        // Метод для загрузки данных из базы
        public void LoadEvents()
        {
            IsCategoryButtonVisible = true;
            IsUnitButtonVisible = true;
            IsToolBarVisible = true;

            Events.Clear();

            // Получаем данные из базы
            var eventsFromDb = GetEvents();

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

        // Загрузка категорий из базы данных
        public List<EventsModelForView> GetEvents()
        {
            var events = new List<EventsModelForView>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT id, DateEvent, Unit, Category, Description, Action, DateCreate, Creator  FROM vwEvents";
                var command = new SQLiteCommand(query, connection);
                using (var reader = command.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("id");
                    int dateIndex = reader.GetOrdinal("DateEvent");
                    int unitIndex = reader.GetOrdinal("Unit");
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


        private void UnitMenuItem(object parameter)
        {
            var unitViewModel = new UnitViewModel(this);
            UnitsView unitsView = new UnitsView(unitViewModel);
            unitsView.Closed += UnitsView_Closed; // Подписываемся на событие Closed окна UnitsView

            if (unitsView.ShowDialog() == true) { }
        }

        private void UnitsView_Closed(object sender, EventArgs e)
        {
            LoadEvents();
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
            LoadEvents();
        }

        private void SettingsMenuItem(object parameter)
        {
            // Создаем и показываем окно настроек
            SettingsWindow settingsWindow = new SettingsWindow(this);
            if (settingsWindow.ShowDialog() == true) { }
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
