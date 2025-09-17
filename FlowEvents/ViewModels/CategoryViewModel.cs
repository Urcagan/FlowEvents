using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class CategoryViewModel : INotifyPropertyChanged
    {
        private readonly ICategoryRepository _categoryRepository;

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                _isSaving = value;
            } 
        }

        private string _connectionString;

        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                // Загрузка данных из базы
       //         LoadCategories();
            }
        }
        //private string _connectionString = "Data Source=G:\\VS Dev\\FlowEvents\\FlowEvents.db;Version=3;foreign keys=true;";

        // Коллекция для хранения категорий (источник данных (коллекцию))
        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();

        // Команды для добавления, редактирования и удаления
        public RelayCommand AddCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand UpdateCommand { get; }


        public CategoryViewModel(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;


            // Инициализация команд
            AddCommand = new RelayCommand(AddCategory);
            CancelCommand = new RelayCommand(CancelEdit);
       //     SaveCommand = new RelayCommand(SaveNewCategory);
            SaveCommand = new RelayCommand(async (object param) => await SaveNewCategoryAsync(), (param) => !string.IsNullOrWhiteSpace(Name));
            DeleteCommand = new RelayCommand(DeleteCategory, CanEditOrDelete);
            UpdateCommand = new RelayCommand(UpdateCategoty, CanEditOrDelete);
            //Как работает RelayCommand?
            //Он принимает два делегата:
            //  Action<object> — метод, который выполняется при вызове команды.
            //  Func<object, bool>(опционально) — метод, который проверяет, можно ли выполнить команду.


            ConnectionString = Global_Var.ConnectionString; //_mainViewModel._connectionString; // $"Data Source={_mainViewModel.appSettings.pathDB};Version=3;foreign keys=true;";

            Categories.Clear();
            // Автоматическая загрузка при создании
            _ = InitializeAsync(); // Асинхронная загрузка категорий

            IsAddButtonVisible = true; // Показать кнопку "Добавить"
            IsDeleteButtonVisible = false; // Скрыть кнопку "Удалить"
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadCategoriesAsync();
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Debug.WriteLine($"Ошибка инициализации: {ex.Message}");
            }
        }

        // Загрузка категорий при инициализации
        private async Task LoadCategoriesAsync()
        {
            try
            {
                Categories = await _categoryRepository.GetAllCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
                Categories = new ObservableCollection<Category>(); // На случай ошибки
            }
        }



        //Сохранение в БД новой категории

        private async Task SaveNewCategoryAsync()
        {
            // Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowErrorMes("Название обязательно для заполнения!");
                return;
            }

            IsSaving = true;
            
            try
            {
                // Проверка на уникальность через репозиторий
                if (!await _categoryRepository.IsCategoryNameUniqueAsync(Name))
                {
                    ShowErrorMes("Категория с таким именем уже существует!");
                    return;
                }

                // Объект для новой категории
                var newCategory = new Category
                {
                    Name = Name,
                    Description = Description,
                    Colour = Colour
                };

                // Сохранение через репозиторий
                var savedCategory = await _categoryRepository.CreateCategoryAsync(newCategory);

                // Обновление списка в UI
                Categories.Add(savedCategory);
                // SelectedCategory = savedCategory;
                
                CancelEdit(null); // Сбрасываем форму
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

        // Обновление 
        private void UpdateCategoty(object parameter)
        {
            if (SelectedCategory == null) return;

            //Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowError("Название обязательно для заполнения!");
                return;
            }

            //// Проверка на уникальность
            //if (!IsCategoryNameUnique(Name))
            //{
            //    ShowError("Категория с таким именем уже существует!");
            //    return;
            //}

            // Обновляем данные выбранной записи
            SelectedCategory.Name = Name;
            SelectedCategory.Description = Description;
            SelectedCategory.Colour = Colour;

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "UPDATE Category SET Name = @Name, Description = @Description, Colour = @Colour WHERE Id = @Id",
                        connection);
                    command.Parameters.AddWithValue("@Name", SelectedCategory.Name);
                    command.Parameters.AddWithValue("@Description", SelectedCategory.Description);
                    command.Parameters.AddWithValue("@Colour", SelectedCategory.Colour);
                    command.Parameters.AddWithValue("@Id", SelectedCategory.Id);
                    command.ExecuteNonQuery();
                }
                OnPropertyChanged(nameof(SelectedCategory)); // Уведомляем об изменении свойств
            }
            catch (Exception ex)
            {
               // MessageBox.Show($"Ошибка данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ShowErrorMes($"Ошибка данных: {ex.Message}");
            }
            SelectedCategory = null; // Снимаем выделение строки
            CancelEdit(null); //Очищаем и закрываем поле редактирования
        }

        // Удаление категории
        private void DeleteCategory(object parameter)
        {
            if (SelectedCategory == null) return;

            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить категорию {SelectedCategory.Name} ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    var command = new SQLiteCommand("DELETE FROM Category WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", SelectedCategory.Id);
                    command.ExecuteNonQuery();
                }
                Categories.Remove(SelectedCategory);
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                // На случай, если FOREIGN_KEY сработал 
                MessageBox.Show(
                    "Невозможно удалить категорию: она используется в записях событий !",
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
        private void AddCategory(object parameter)
        {
            // Очистите поля ввода (если нужно)
            Name = string.Empty;
            Description = string.Empty;
            Colour = string.Empty;

            IsEditPanelVisible = true; //показываем панель редактирования
            // Управление видимостью кнопок
            IsCreateButtonVisible = true;  // Показать кнопку "Создать"
            IsUpdateButtonVisible = false; // Скрыть кнопку "Обновить"
            IsAddButtonVisible = false; // Скрыть кнопку "Добавить"
            SelectedCategory = null; // Снимаем выделение строки
        }

        // Проверка ктегории на уникальность
        private bool IsCategoryNameUnique(string name)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("SELECT COUNT(*) FROM Category WHERE Name = @Name", connection);
                    command.Parameters.AddWithValue("@Name", name);
                    return Convert.ToInt32(command.ExecuteScalar()) == 0;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Ошибка данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

        }


        // ===================================================================================================
        // Методы для отображения ошибки и очистки поля с текстом ошибки.

        private void ShowErrorMes(string message) // Метод отображение ошибки в виде сообщения
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

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
            // Очистите поля ввода (если нужно)
            Name = string.Empty;
            Description = string.Empty;
            Colour = string.Empty;
            IsEditPanelVisible = false; // Скрыть панель редактирования
            SelectedCategory = null; // Снимаем выделение строки
            IsAddButtonVisible = true; // Показать кнопку "Добавить"
        }

        // Проверка, можно ли редактировать или удалять
        private bool CanEditOrDelete(object parameter)
        {
            return SelectedCategory != null;
        }

        //====================================================================================================
        //Поля, Переменный, Свойства

        //----------------------------------------
        // свойства для привязки к полям ввода и редактирования данных.
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();

                    // Очищаем ошибку при изменении текста
                    ClearError();
                }
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        private string _colour;
        public string Colour
        {
            get { return _colour; }
            set
            {
                _colour = value;
                OnPropertyChanged();
            }
        }

        // Объект для размещения данных выделенной строки. При выборе строки таблицы в переменную поместятся все значения выделенной строки
        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();

                // В случае если выбрана какая либо строка , то загружаем данные этой троки в поля для редактирования и отображаем окно редактирования
                if (_selectedCategory != null)
                {
                    // Заполните поля данными выбранной категории
                    Name = _selectedCategory.Name;
                    Description = _selectedCategory.Description;
                    Colour = _selectedCategory.Colour;

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
        private bool _isEditPanelVisible; // поле состояния видимости окна редактиорвания
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

        // Метод для генерации события
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
