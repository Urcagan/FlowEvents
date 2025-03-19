using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace FlowEvents
{
    public class UnitViewModel : INotifyPropertyChanged
    {
        private string _connectionString = $"Data Source={Global_Var.pathDB};Version=3;";

        // Коллекция для хранения категорий (источник данных (коллекцию))
        public ObservableCollection<UnitModel> Units { get; set; } = new ObservableCollection<UnitModel>();

        // Команды для добавления, редактирования и удаления
        public RelayCommand AddCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand UpdateCommand { get; }


        public UnitViewModel()
        {
            // Инициализация команд
            AddCommand = new RelayCommand(AddUnit);
            CancelCommand = new RelayCommand(CancelEdit);
            SaveCommand = new RelayCommand(SaveNewUnit);
            DeleteCommand = new RelayCommand(DeleteUnit, CanEditOrDelete);
            UpdateCommand = new RelayCommand(UpdateUnit, CanEditOrDelete);

            // Загрузка данных из базы
            GetUnits();
            IsAddButtonVisible = true; // Показать кнопку "Добавить"
            IsDeleteButtonVisible = false; // Скрыть кнопку "Удалить"


        }



        // Метод для загрузки данных из таблицы Units
        private void GetUnits()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("SELECT * FROM Units", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        int idIndex = reader.GetOrdinal("id");
                        int unitIndex = reader.GetOrdinal("Unit");
                        int descriptionIndex = reader.GetOrdinal("Description");

                        while (reader.Read())
                        {
                            var unit = new UnitModel
                            {
                                Id = reader.GetInt32(idIndex),
                                Unit = reader.GetString(unitIndex),
                                Description = reader.IsDBNull(descriptionIndex) ? null : reader.GetString(descriptionIndex)
                            };
                            Units.Add(unit);
                        }
                    }
                }
            }
            catch (SQLiteException ex)
            {
                // Обработка ошибок, связанных с SQLite
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Сохранение в БД новой категории
        private void SaveNewUnit(object parameter)
        {
            // Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(Unit))
            {
                ShowError("Название обязательно для заполнения!");
                // ClearField(NameTextBox);
                return;
            }

            // Проверка на уникальность
            if (!IsUnitUnique(Unit))
            {
                ShowError("Категория с таким именем уже существует!");
                // ClearField(NameTextBox);
                return;
            }

            // Создание экземпляра для хранения нового Юнита
            var newUnit = new UnitModel
            {
                Unit = Unit,
                Description = Description
            };

            // Сохранение в базу
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "INSERT INTO Units (Unit, Description) " +
                        "VALUES (@Unit, @Description)",
                        connection);

                    command.Parameters.AddWithValue("@Unit", newUnit.Unit);
                    command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(newUnit.Description) ? DBNull.Value : (object)newUnit.Description);
                    command.ExecuteNonQuery();

                    long newId = connection.LastInsertRowId; // Получаем ID новой записи
                    newUnit.Id = (int)newId; // Присваиваем ID новой записи объекту newUnit
                }

                // Обновление списка
                Units.Add(newUnit);
                //SelectedUnit = newUnit;
            }
            catch (SQLiteException ex)
            {
                // Обработка ошибок, связанных с SQLite
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            CancelEdit(null); //Очищаем и закрываем поле редактирования
        }

        //Обновление записи
        private void UpdateUnit(object obj)
        {
            if (SelectedUnit == null) return;

            // Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(Unit))
            {
                ShowError("Название обязательно для заполнения!");
                return;
            }
            
            // Проверка на уникальность
            //if (!IsUnitUnique(Unit))
            //{
            //    ShowError("Категория с таким именем уже существует!");
            //    return;
            //}

            // Обновляем данные выбранной записи
            SelectedUnit.Unit = Unit;
            SelectedUnit.Description = Description;

            try
            {
                // Обновление в базе данных
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "UPDATE Units SET Unit = @Unit, Description = @Description WHERE Id = @Id",
                        connection);
                    command.Parameters.AddWithValue("@Unit", SelectedUnit.Unit);
                    command.Parameters.AddWithValue("@Description", SelectedUnit.Description);
                    command.Parameters.AddWithValue("@Id", SelectedUnit.Id);
                    command.ExecuteNonQuery();
                }
                OnPropertyChanged(nameof(SelectedUnit)); // Уведомляем об изменении свойств
            }
            catch (SQLiteException ex)
            {
                // Обработка ошибок, связанных с SQLite
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SelectedUnit = null; // Снимаем выделение строки
            CancelEdit(null); //Очищаем и закрываем поле редактирования
        }

        // Удаление категории
        private void DeleteUnit(object parameter)
        {
            if (SelectedUnit == null) return;

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("DELETE FROM Units WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", SelectedUnit.Id);
                    command.ExecuteNonQuery();
                }

                Units.Remove(SelectedUnit);
            }
            catch (SQLiteException ex)
            {
                // Обработка ошибок, связанных с SQLite
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            CancelEdit(null); //Очищаем и закрываем поле редактирования
        }


        // ===================================================================================================
        // Вспомогательные методы

        // Открытия доступа к полям ввода новой категории
        private void AddUnit(object parameter)
        {
            // Очистите поля ввода (если нужно)
            Unit = string.Empty;
            Description = string.Empty;

            IsEditPanelVisible = true; //показываем панель редактирования

            // Управление видимостью кнопок
            IsCreateButtonVisible = true;  // Показать кнопку "Создать"
            IsUpdateButtonVisible = false; // Скрыть кнопку "Обновить"
            IsAddButtonVisible = false; // Скрыть кнопку "Добавить"
            SelectedUnit = null; // Снимаем выделение строки
        }

        // Проверка на уникальность
        private bool IsUnitUnique(string name)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("SELECT COUNT(*) FROM Units WHERE Unit = @Name", connection);
                    command.Parameters.AddWithValue("@Name", name);
                    return Convert.ToInt32(command.ExecuteScalar()) == 0;
                }
            }
            catch (SQLiteException ex)
            {
                // Обработка ошибок, связанных с SQLite
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                // Обработка всех остальных ошибок
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        // ===================================================================================================
        // Методы для отображения ошибки и очистки поля с текстом ошибки.
        private void ShowError(string errorMessage)
        {
            ErrorText = errorMessage;
            IsErrorTextVisible = true;
        }

        private void ClearError()
        {
            ErrorText = string.Empty;
            IsErrorTextVisible = false;
        }

        // Закрытие доступа к полям ввода данных
        private void CancelEdit(object parameter)
        {
            // Очистите поля ввода
            Unit = string.Empty;
            Description = string.Empty;
            IsEditPanelVisible = false; // Скрыть панель редактирования
            SelectedUnit = null; // Снимаем выделение строки
            IsAddButtonVisible = true; // Показать кнопку "Добавить"
        }

        // Проверка, можно ли редактировать или удалять
        private bool CanEditOrDelete(object parameter)
        {
            return SelectedUnit != null;
        }


        //===============================================================================================================================================
        //Поля, Переменный, Свойства

        //----------------------------------------
        // свойства для привязки к полям ввода и редактирования данных.
        private string _unit;
        public string Unit
        {
            get => _unit;
            set
            {
                if (_unit != value)
                {
                    _unit = value;
                    OnPropertyChanged();

                    // Очищаем поле с текстом ошибки при изменении текста
                    ClearError();
                }
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
            }
        }


        // Объект для размещения данных выделенной строки. При выборе строки таблицы в переменную поместятся все значения выделенной строки
        private UnitModel _selectedUnit;
        public UnitModel SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged();

                // В случае если выбрана какая либо строка , то загружаем данные этой троки в поля для редактирования и отображаем окно редактирования
                if (_selectedUnit != null)
                {
                    // Заполните поля данными выбранной категории
                    Unit = _selectedUnit.Unit;
                    Description = _selectedUnit.Description;

                    // Покажите правую панель
                    IsEditPanelVisible = true;

                    // Управление видимостью кнопок
                    IsCreateButtonVisible = false; // Скрыть кнопку "Создать"
                    IsUpdateButtonVisible = true;  // Показать кнопку "Обновить"
                    IsAddButtonVisible = false; // Скрыть кнопку "Добавить"
                    IsDeleteButtonVisible = true; // Показать кнопку "Удалить"
                }
                else
                {
                    // Если строка не выбрана, скрываем кнопку "Удалить"
                    IsDeleteButtonVisible = false;
                }
            }
        }

        // Свойство содержащее состояния видимости окна редактиорвания 
        private bool _isEditPanelVisible;
        public bool IsEditPanelVisible
        {
            get { return _isEditPanelVisible; }
            set
            {
                _isEditPanelVisible = value;
                OnPropertyChanged();
            }
        }

        // Свойство ErrorText, которое будет хранить текст ошибки  
        private string _errorText;
        public string ErrorText
        {
            get => _errorText;
            set
            {
                _errorText = value;
                OnPropertyChanged();
            }
        }

        //Свойство IsErrorTextVisible, которое будет управлять видимостью TextBlock с содержимым строки ошибки.
        private bool _isErrorTextVisible;
        public bool IsErrorTextVisible
        {
            get => _isErrorTextVisible;
            set
            {
                _isErrorTextVisible = value;
                OnPropertyChanged();
            }
        }


        //====================================================================================================
        //Свойство для управления видимостью кнопок

        //Кнопка Создать
        private bool _isCreateButtonVisible;
        public bool IsCreateButtonVisible
        {
            get => _isCreateButtonVisible;
            set
            {
                _isCreateButtonVisible = value;
                OnPropertyChanged();
            }
        }

        //Кнопка Обновить
        private bool _isUpdateButtonVisible;
        public bool IsUpdateButtonVisible
        {
            get => _isUpdateButtonVisible;
            set
            {
                _isUpdateButtonVisible = value;
                OnPropertyChanged();
            }
        }

        //Кнопка Добавить
        private bool _isAddButtonVisible; // Видимость кнопки "Добавить"
        public bool IsAddButtonVisible
        {
            get => _isAddButtonVisible;
            set
            {
                _isAddButtonVisible = value;
                OnPropertyChanged();
            }
        }

        //Кнопка Удалить
        private bool _isDellButtonVisible;   // Видимость кнопки "Удалить"
        public bool IsDeleteButtonVisible
        {
            get { return _isDellButtonVisible; }
            set
            {
                _isDellButtonVisible = value;
                OnPropertyChanged(nameof(IsDeleteButtonVisible));
            }
        }


        // Реализация INotifyPropertyChanged
        // Событие, которое уведомляет об изменении свойства
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
