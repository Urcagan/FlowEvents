using FlowEvents.Models;
using FlowEvents.Repositories.Implementations;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
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
        #region
        private readonly IUnitRepository _unitRepository;
        private bool _isSaving;
        private bool _isDeleting;
        private bool _isLoading;
        private bool _visibleBar;

        private ObservableCollection<Unit> _units { get; set; } = new ObservableCollection<Unit>();


        private string _connectionString;
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                // OnPropertyChanged(nameof(ConnectionString));
               // GetUnits();
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
                VisibleBar = value;
                OnPropertyChanged();
                // UpdateCanExecute();
                // SaveCommand.RaiseCanExecuteChanged(); // ✅ Обновит кнопку
            }
        }

        public bool IsDeleting
        {
            get => _isDeleting;
            set
            {
                _isDeleting = value;
                VisibleBar = value;
                OnPropertyChanged();
                // UpdateCanExecute();
            }
        }
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                VisibleBar = value;
                OnPropertyChanged();
            }
        }

        public bool VisibleBar
        {
            get => _visibleBar;
            set
            {
                _visibleBar = value;
                OnPropertyChanged();
            }
        }
        

        public ObservableCollection<Unit> Units // Коллекция для хранения категорий (источник данных (коллекцию))
        {
            get => _units;
            set
            {
                _units = value;
                OnPropertyChanged(nameof(Units));
            }
        }

        // Команды для добавления, редактирования и удаления
        public RelayCommand AddCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand UpdateCommand { get; }

        #endregion

        public UnitViewModel(IUnitRepository unitRepository)
        {
            _unitRepository = unitRepository;


            // Инициализация команд
            AddCommand = new RelayCommand(AddUnit);
            CancelCommand = new RelayCommand(CancelEdit);
            SaveCommand = new RelayCommand(SaveNewUnit);
            DeleteCommand = new RelayCommand(DeleteUnit, CanEditOrDelete);
            UpdateCommand = new RelayCommand(UpdateUnit, CanEditOrDelete);

            // Загрузка данных из базы
            ConnectionString = Global_Var.ConnectionString; //_mainViewModel._connectionString; //$"Data Source={_mainViewModel.appSettings.pathDB};Version=3;";

            _ = InitializeAsync(); // Асинхронная загрузка данных из БД

            IsAddButtonVisible = true; // Показать кнопку "Добавить"
            IsDeleteButtonVisible = false; // Скрыть кнопку "Удалить"
        }


        private async Task InitializeAsync()
        {
            try
            {
                await LoadUnitsAsync();
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }
        }

        // Метод для загрузки данных из таблицы Units

        private async Task LoadUnitsAsync()
        {
            IsLoading = true;

            await Task.Delay(500); // Имитация задержки сети (2 секунды)

            try
            {
                Units = await _unitRepository.GetAllUnitsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки : {ex.Message}");
                Units = new ObservableCollection<Unit>(); // На случай ошибки
            }
            finally
            {
                IsLoading = false;
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
            var newUnit = new Unit
            {
                UnitName = Unit,
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

                    command.Parameters.AddWithValue("@Unit", newUnit.UnitName);
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


            // Обновляем данные выбранной записи
            SelectedUnit.UnitName = Unit;
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
                    command.Parameters.AddWithValue("@Unit", SelectedUnit.UnitName);
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

            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить объект {SelectedUnit.UnitName} ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

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
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                // На случай, если FOREIGN_KEY сработал 
                MessageBox.Show(
                    "Невозможно удалить объект: он используется в записях событий !",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
        private Unit _selectedUnit;
        public Unit SelectedUnit
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
                    Unit = _selectedUnit.UnitName;
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
