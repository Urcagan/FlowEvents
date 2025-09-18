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
            SaveCommand = new RelayCommand( async () => await SaveNewUnitAsync(), () => CanExecuteSave());
            UpdateCommand = new RelayCommand(async () => await UpdateAsync(), () => CanExecuteUpdate());
            DeleteCommand = new RelayCommand(async () => await DeleteUnitAsync(), () =>  CanExecuteDelete());
            

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
        private async Task SaveNewUnitAsync()
        {
            // Проверка на пустое значение
              if (string.IsNullOrWhiteSpace(Unit))
            {
                ShowErrorMes("Название обязательно для заполнения!");
                // ClearField(NameTextBox);
                return;
            }
            IsSaving = true;

            await Task.Delay(500); // Имитация задержки сети (2 секунды)

            try
            {
                if (!await _unitRepository.IsUnitNameUniqueAsync(Unit)) // Проверка на уникальность
                {
                    ShowErrorMes("Объект с таким именем уже существует!");
                    return;
                }

                // Создание экземпляра для хранения нового Юнита
                var newUnit = new Unit
                {
                    UnitName = Unit,
                    Description = Description
                };

                var savedUnit = await _unitRepository.CreateUnitAsync(newUnit);
                                
                Units.Add(savedUnit); // Обновление списка в UI

                CancelEdit();
            }
            catch (Exception ex)
            {
                ShowErrorMes($"Ошибка данных: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }


     

        //Обновление записи

        private async Task UpdateAsync()
        {
            if (SelectedUnit == null) return;

            // Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(Unit))
            {
                ShowErrorMes("Название обязательно для заполнения!");
                return;
            }

            // Проверка на уникальность (исключая текущую категорию)
            if (!await _unitRepository.IsUnitNameUniqueAsync(Unit, SelectedUnit.Id))
            {
                ShowErrorMes("Объект с таким именем уже существует!");
                return;
            }

            IsSaving = true;
            await Task.Delay(500); // Имитация задержки сети

            try
            {
                // Обновляем данные выбранной записи
                SelectedUnit.UnitName = Unit;
                SelectedUnit.Description = Description;

                // Сохраняем через репозиторий
                var updatedUnit = await _unitRepository.UpdateUnitAsync(SelectedUnit);

                // Обновляем UI
                var index = Units.IndexOf(SelectedUnit);
                if (index >= 0)
                {
                    Units[index] = updatedUnit;
                }

                OnPropertyChanged(nameof(SelectedUnit)); // Уведомляем об изменении свойств
                ShowSuccess("Категория успешно обновлена!");
            }
            catch (Exception ex)
            {
                ShowErrorMes($"Ошибка данных: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
            SelectedUnit = null; // Снимаем выделение строки
            CancelEdit(); //Очищаем и закрываем поле редактирования
        }

       

        // Удаление категории
        private async Task DeleteUnitAsync()
        {
            if (SelectedUnit == null) return;

            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить объект {SelectedUnit.UnitName} ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            IsDeleting = true;

            await Task.Delay(500); // Имитация задержки сети (2 секунды)
            
            try
            {
                var success = await _unitRepository.DeleteUnitAsync(SelectedUnit.Id);

                if (success)
                {
                    string _name = SelectedUnit.UnitName;
                    Units.Remove(SelectedUnit);
                    ShowSuccess($"Объект '{_name}' успешно удален!");
                }
                else
                {
                    ShowErrorMes("Объект не найден или уже была удален.");
                }

            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("используется"))
            {
                // На случай, если FOREIGN_KEY сработал 
                ShowErrorMes($"Невозможно удалить объект: он используется в записях событий!");
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                ShowErrorMes($"Невозможно удалить объект: он используется в других записях!");
            }
            catch (Exception ex)
            {
                ShowErrorMes($"Ошибка при удалении: {ex.Message}");
            }
            finally
            {
                IsDeleting = false;
            }
            CancelEdit(); //Очищаем и закрываем поле редактирования
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

       

        // ===================================================================================================
        // Методы для отображения ошибки и очистки поля с текстом ошибки.
        private void ShowErrorMes(string message) // Метод отображение ошибки в виде сообщения
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Закрытие доступа к полям ввода данных
        private void CancelEdit()
        {
            // Очистите поля ввода
            Unit = string.Empty;
            Description = string.Empty;
            IsEditPanelVisible = false; // Скрыть панель редактирования
            SelectedUnit = null; // Снимаем выделение строки
            IsAddButtonVisible = true; // Показать кнопку "Добавить"
        }

        private bool CanExecuteSave()
        {
            return !IsSaving && !string.IsNullOrWhiteSpace(Unit);
        }
        private bool CanExecuteUpdate()
        {
            return !IsSaving && SelectedUnit != null && !string.IsNullOrWhiteSpace(Unit);
        }
        // Проверка, можно ли редактировать или удалять
        private bool CanExecuteDelete()
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
