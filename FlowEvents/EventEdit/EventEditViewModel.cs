using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FlowEvents
{
    public class EventEditViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _mainViewModel;
        private readonly EventsModelForView _originalEvent;

        private string _connectionString;
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
            }
        }

        public int Id { get; }

        public ObservableCollection<UnitModel> Units { get; } = new ObservableCollection<UnitModel>();
        public ObservableCollection<CategoryModel> Categories { get; } = new ObservableCollection<CategoryModel>();

        // Свойство с Id выбранных элементов
        public List<int> SelectedIds => Units.Where(u => u.IsSelected).Select(u => u.Id).ToList();

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

        public RelayCommand SaveUpdateCommand { get; }
        public RelayCommand CancelCommand { get; }


        public EventEditViewModel(MainViewModel mainViewModel, EventsModelForView eventToEdit)
        {
            _mainViewModel = mainViewModel;
            _originalEvent = eventToEdit;

            ConnectionString = $"Data Source={_mainViewModel.appSettings.pathDB};Version=3;";

            Id = eventToEdit.Id;
            SelectedDateEvent = eventToEdit.DateEvent;
            Refining = eventToEdit.OilRefining;
            Description = eventToEdit.Description;
            Action = eventToEdit.Action;

            GetUnitFromDatabase(); //Получаем элементы УСТАНОВКА из БД

            LoadSelectedUnitsForEvent(Id);

            Categories.Insert(0, new CategoryModel { Id = -1, Name = "Выбор события" });
            GetCategoryFromDatabase();
            SelectedCategory = Categories.FirstOrDefault(c => c.Name == eventToEdit.Category);  //Categories.FirstOrDefault();

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

            SaveUpdateCommand = new RelayCommand(SaveUpdatedEvents);
            CancelCommand = new RelayCommand(Cancel);
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

        // метод полусения из базы, установок связанных с событием 
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

        private void SaveUpdatedEvents(object parameters)
        {
            //if (!ValidateEvent()) return;

            // Создание экземпляра для хранения нового Event
            var _updateEvent = new EventsModel
            {
                //DateEvent = DatePicker.SelectedDate?.ToString(formatDate) ?? DateTime.Now.ToString(formatDate),
                Id = this.Id,
                DateEvent = SelectedDateEvent.ToString(AppBaseConfig.formatDate),
                OilRefining = Refining,
                Id_Category = _selectedCategory.Id,
                Description = Description,
                Action = Action

            };
            EditEvent(_updateEvent, SelectedIds);
            CloseWindow();
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
                            UpdateEvent(connection, updateEvent);

                            // надо переписать для записи нескольких ID
                            UpdateEventUnit(connection, Id, selectedIds);

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

        private void UpdateEvent(SQLiteConnection connection, EventsModel updateEvent)
        {
            // SQL-запрос для вставки данных
            var query = "UPDATE Events SET DateEvent = @DateEvent, OilRefining = @OilRefining , id_category = @id_category, Description = @Description, Action = @Action WHERE id = @SelectedRowId ";

            using (var command = new SQLiteCommand(query, connection))
            {
                // Добавление параметров
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

        //свойство, которое проверяет все обязательные поля:
        public bool CanSave
        {
            get
            {
                return SelectedIds != null && SelectedIds.Count > 0 &&
                       SelectedCategory != null && SelectedCategory.Id != -1 &&
                       !string.IsNullOrWhiteSpace(Description);
            }
        }

        private void UpdateSelectedUnitsText()
        {
            var selectedUnits = Units.Where(u => u.IsSelected).Select(u => u.Unit);
            SelectedUnitsText = selectedUnits.Any()
                ? $"Выбрано: {string.Join(", ", selectedUnits)}"
                : "Ничего не выбрано";
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

        //---------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
