using FlowEvents.Models;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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


        //----------------------------------------
        // свойства для привязки к полям ввода и редактирования данных...
        //private UnitModel _selectedUnit;
        //public UnitModel SelectedUnit
        //{
        //    get => _selectedUnit;
        //    set
        //    {
        //        _selectedUnit = value;
        //        OnPropertyChanged();
        //        OnPropertyChanged(nameof(CanSave));
        //    }
        //}

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

        // Коллекция выбранных объектов (можно привязать к UI или использовать в коде)
        //public ObservableCollection<UnitModel> SelectedUnits =>
        //    new ObservableCollection<UnitModel>(Units.Where(u => u.IsSelected));

        // Свойство с Id выбранных элементов
        public List<int> SelectedIds => Units.Where(u => u.IsSelected).Select(u => u.Id).ToList();

        //----------------------------------------

        private readonly CultureInfo culture = AppBaseConfig.culture;   // Культура русского языка
        private readonly string formatDate = AppBaseConfig.formatDate; //Формат даты в приложении
        private readonly string formatDateTime = AppBaseConfig.formatDateTime; // формат даты с временем

        // Команды взаимодействия с view
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public EventAddViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            ConnectionString = $"Data Source={_mainViewModel.appSettings.pathDB};Version=3;";

            SaveCommand = new RelayCommand(SaveNewEvent);
            CancelCommand = new RelayCommand(Cancel);

           
            GetUnitFromDatabase(); //Получаем элементы УСТАНОВКА из БД

            Categories.Insert(0, new CategoryModel { Id = -1, Name = "Выбор события" });
            SelectedCategory = Categories.FirstOrDefault();
            GetCategoryFromDatabase();

            // Установка текущей даты по умолчанию
            SelectedDateEvent = DateTime.Now;


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
            /**
            // Проверка выбранной установки
            if (SelectedUnit == null || SelectedUnit.Id == -1)
            {
                MessageBox.Show("Пожалуйста, выберите установку из списка");
                return;
            }

            // Проверка выбранной категории
            if (SelectedCategory == null || SelectedCategory.Id == -1)
            {
                MessageBox.Show("Пожалуйста, выберите категорию события");
                return;
            }

            // Проверка описания события
            if (string.IsNullOrWhiteSpace(Description))
            {
                MessageBox.Show("Описание события не может быть пустым");
                return;
            }
            **/

            if (!ValidateEvent()) return;

            // Создание экземпляра для хранения нового Event
            var newEvent = new EventsModel
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

            AddEvent(newEvent);

            CloseWindow();

     
        }

        //Валидация перед сохранением:
        private bool ValidateEvent()
        {
            // Проверка выбранной установки
            if (SelectedIds == null || SelectedIds.Count == 0 )
            {
                MessageBox.Show("Не выбрана установка");
                return false;
            }

            // Проверка выбранной категории
            if (SelectedCategory == null || SelectedCategory.Id == -1)
            {
                MessageBox.Show("Не выбрана категория");
                return false;
            }

            // Проверка описания события
            if (string.IsNullOrWhiteSpace(Description))
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

        //свойство, которое проверяет все обязательные поля:
        public bool CanSave
        {
            
            get
            {
                //MessageBox.Show("CanSave");
                return SelectedIds != null &&  SelectedIds.Count > 0 &&
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
