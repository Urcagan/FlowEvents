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


        private UnitModel _selectedUnit;
        public UnitModel SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }

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


        public RelayCommand SaveUpdateCommand { get;}
        public RelayCommand CancelCommand { get;}


        public EventEditViewModel(MainViewModel mainViewModel, EventsModelForView eventToEdit)
        {
            _mainViewModel = mainViewModel;
            _originalEvent = eventToEdit;

            SaveUpdateCommand = new RelayCommand(SaveUpdatedEvents);
            CancelCommand = new RelayCommand(Cancel);
            
            
            ConnectionString = $"Data Source={_mainViewModel.appSettings.pathDB};Version=3;";

            Id = eventToEdit.Id;
            SelectedDateEvent = eventToEdit.DateEvent;
            Description = eventToEdit.Description;
            Action = eventToEdit.Action;

            // Добавляем первый элемент в коллекцию для отображения первым в ComboВox
            Units.Insert(0, new UnitModel { Id = -1, Unit = "Выбр объекта" });
            GetUnitFromDatabase(); //Получаем элементы УСТАНОВКА из БД
            SelectedUnit = Units.FirstOrDefault(u => u.Unit == eventToEdit.Unit); //Units.FirstOrDefault();
            

            Categories.Insert(0, new CategoryModel { Id = -1, Name = "Выбор события" });
            GetCategoryFromDatabase();
            SelectedCategory = Categories.FirstOrDefault(c => c.Name == eventToEdit.Category);  //Categories.FirstOrDefault();
            



            //SaveCommand = new RelayCommand(SaveChanges);
            //CancelCommand = new RelayCommand(Cancel);


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
                Id_Category = _selectedCategory.Id,
                Description = Description,
                Action = Action
            };
            EditEvent(_updateEvent);
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


        public void EditEvent(EventsModel updateEvent)
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

                            UpdateEventUnit(connection, Id, _selectedUnit.Id);

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
            var query = "UPDATE Events SET DateEvent = @DateEvent, id_category = @id_category, Description = @Description, Action = @Action WHERE id = @SelectedRowId ";

            using (var command = new SQLiteCommand(query, connection))
            {
                // Добавление параметров
                command.Parameters.AddWithValue("@DateEvent", updateEvent.DateEvent);
                command.Parameters.AddWithValue("@id_category", updateEvent.Id_Category);
                command.Parameters.AddWithValue("@Description", updateEvent.Description ?? (object)DBNull.Value); // Если Description == null, вставляем NULL
                command.Parameters.AddWithValue("@Action", updateEvent.Action ?? (object)DBNull.Value); // Если Action == null, вставляем NULL
                command.Parameters.AddWithValue("@SelectedRowId", updateEvent.Id);
                command.ExecuteNonQuery();
            }
        }


        private void UpdateEventUnit(SQLiteConnection connection, long eventId, int unitId)
        {
            var delQuery = "DELETE FROM EventUnits WHERE EventID = @EventId;";
            using(var command = new SQLiteCommand(delQuery, connection))
            {
                command.Parameters.AddWithValue("@EventId", eventId);
                command.ExecuteNonQuery();
            }
            

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
                return SelectedUnit != null && SelectedUnit.Id != -1 &&
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

        //---------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
