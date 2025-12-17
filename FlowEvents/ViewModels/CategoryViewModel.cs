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
using System.Windows.Input;

namespace FlowEvents
{
    public class CategoryViewModel : INotifyPropertyChanged
    {
        private readonly ICategoryRepository _categoryRepository;
        private bool _isSaving;
        private bool _isDeleting;
        private bool _isLoading;
        private bool _visibleBar;
        private string _name;
        private string _description;
        private string _colour;
        private Category _selectedCategory; // Данные выделенной строки.


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

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        public string Colour
        {
            get { return _colour; }
            set
            {
                _colour = value;
                OnPropertyChanged();
            }
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                LoadPropertiesSelectedRow(); // Загружаем данные этой cтроки в поля для редактирования и отображаем окно редактирования
            }
        }



        // Коллекция для хранения категорий (источник данных (коллекцию))
        private ObservableCollection<Category>_categories { get; set; } = new ObservableCollection<Category>();

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set
            {
                _categories = value;
                OnPropertyChanged(nameof(Categories));
            }
        }

        // Команды для добавления, редактирования и удаления
        public RelayCommand AddCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand UpdateCommand { get; }
        public RelayCommand EditCommand {  get; }


        public CategoryViewModel(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;

            // Инициализация команд
            AddCommand = new RelayCommand(AddCategory);
            CancelCommand = new RelayCommand(CancelEdit);
            SaveCommand = new RelayCommand(async () => await SaveNewCategoryAsync(), () => CanExecuteSave());
            UpdateCommand = new RelayCommand(async () => await UpdateCategoryAsync(), CanExecuteUpdate);
            DeleteCommand = new RelayCommand(async () => await DeleteCategoryAsync());
            EditCommand = new RelayCommand(EditCategory);
  
            //Как работает RelayCommand?
            //Он принимает два делегата:
            //  Action<object> — метод, который выполняется при вызове команды.
            //  Func<object, bool>(опционально) — метод, который проверяет, можно ли выполнить команду.

            Categories.Clear();
            // Автоматическая загрузка при создании
            _ = InitializeAsync(); // Асинхронная загрузка данных из БД

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


        // Редактирование категории
        private void EditCategory()
        {
            if (_selectedCategory != null)
            {
                // Заполните поля данными выбранной категории
                Name = _selectedCategory.Name;
                Description = _selectedCategory.Description;
                //Colour = _selectedCategory.Colour;

                // Покажите правую панель
                IsEditPanelVisible = true;
                IsEditMode = true;          //Режим редактирования записи

                // Управление видимостью кнопок
                IsCreateButtonVisible = false; // Скрыть кнопку "Создать"
              //  IsUpdateButtonVisible = true;  // Показать кнопку "Обновить"
                IsAddButtonVisible = false; // Скрыть кнопку "Добавить"
                IsDeleteButtonVisible = true; // Показать кнопку "Удалить"
            }
        }

        // Загрузка категорий при инициализации
        private async Task LoadCategoriesAsync()
        {
            IsLoading = true;
                     
            try
            {
                Categories = await _categoryRepository.GetAllCategoriesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
                Categories = new ObservableCollection<Category>(); // На случай ошибки
            }
            finally
            {
                IsLoading = false;
            }
        }


        //Сохранение в БД новой категории
        private async Task SaveNewCategoryAsync()
        {
            // Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowError("Название обязательно для заполнения!");
                return;
            }

            IsSaving = true;
                        
            try
            {
                // Проверка на уникальность через репозиторий
                if (!await _categoryRepository.IsCategoryNameUniqueAsync(Name))
                {
                    ShowError("Категория с таким именем уже существует!");
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

                CancelEdit(); // Сбрасываем форму
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка данных: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }
        }


        /// Обновление категории
        private async Task UpdateCategoryAsync()
        {
            if (SelectedCategory == null) return;

            // Проверка на пустое значение
            if (string.IsNullOrWhiteSpace(Name))
            {
                ShowError("Название обязательно для заполнения!");
                return;
            }

            // Проверка на уникальность (исключая текущую категорию)
            if (!await _categoryRepository.IsCategoryNameUniqueAsync(Name, SelectedCategory.Id))
            {
                ShowError("Категория с таким именем уже существует!");
                return;
            }

            IsSaving = true;

            try
            {
                // Обновляем данные выбранной записи
                SelectedCategory.Name = Name;
                SelectedCategory.Description = Description;
                SelectedCategory.Colour = Colour;

                // Сохраняем через репозиторий
                var updatedCategory = await _categoryRepository.UpdateCategoryAsync(SelectedCategory);

                // Обновляем UI
                var index = Categories.IndexOf(SelectedCategory);
                if (index >= 0)
                {
                    Categories[index] = updatedCategory;
                }

                OnPropertyChanged(nameof(SelectedCategory)); // Уведомляем об изменении свойств
                ShowSuccess("Категория успешно обновлена!");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка данных: {ex.Message}");
            }
            finally
            {
                IsSaving = false;
            }

            SelectedCategory = null; // Снимаем выделение
            CancelEdit(); // Очищаем форму
        }


        // Удаление категории
        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory == null) return;

            var confirm = MessageBox.Show(
                $"Вы уверены, что хотите удалить категорию '{SelectedCategory.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            IsDeleting = true;

            try
            {
                // Вызываем метод репозитория (возвращает bool)
                var success = await _categoryRepository.DeleteCategoryAsync(SelectedCategory.Id);

                if (success)
                {
                    string _name = SelectedCategory.Name;
                    Categories.Remove(SelectedCategory);
                    ShowSuccess($"Категория '{_name}' успешно удалена!");
                }
                else
                {
                    ShowError("Категория не найдена или уже была удалена.");
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("используется"))
            {
                ShowError($"Невозможно удалить категорию: она используется в записях событий!");
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                ShowError($"Невозможно удалить категорию: она используется в других записях!");
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при удалении: {ex.Message}");
            }
            finally
            {
                IsDeleting = false;
            }

            CancelEdit();
        }


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
           // IsUpdateButtonVisible = false; // Скрыть кнопку "Обновить"
            IsAddButtonVisible = false; // Скрыть кнопку "Добавить"
            SelectedCategory = null; // Снимаем выделение строки
        }



        // ===================================================================================================
        // Методы для отображения ошибки

        private void ShowError(string message) // Метод отображение ошибки в виде сообщения
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
            // Очистите поля ввода (если нужно)
            Name = string.Empty;
            Description = string.Empty;
            Colour = string.Empty;
            IsEditPanelVisible = false; // Скрыть панель редактирования
            IsEditMode = false;          //Режим редактирования записи
            SelectedCategory = null; // Снимаем выделение строки
            IsAddButtonVisible = true; // Показать кнопку "Добавить"
        }

        // Проверка, можно ли редактировать или удалять

        private bool CanExecuteSave()
        {
            return !IsSaving && !string.IsNullOrWhiteSpace(Name);
        }

        private bool CanExecuteUpdate()
        {
            return !IsSaving && SelectedCategory != null && !string.IsNullOrWhiteSpace(Name);
        }

        private bool CanExecuteDelete()
        {
            return !IsDeleting && SelectedCategory != null;
        }

        private void UpdateCanExecute()
        {
            ((RelayCommand)DeleteCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)SaveCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)UpdateCommand)?.RaiseCanExecuteChanged();
        }


        // Загрузить данные выделенной строки в поля редактирования
        private void LoadPropertiesSelectedRow()
        {
            // В случае если выбрана какая либо строка , то загружаем данные этой cтроки в поля для редактирования и отображаем окно редактирования
            if (_selectedCategory != null)
            {
                // Заполните поля данными выбранной категории
                //Name = _selectedCategory.Name;
                //Description = _selectedCategory.Description;
                //Colour = _selectedCategory.Colour;

                // Покажите правую панель
                //IsEditPanelVisible = true;

                // Управление видимостью кнопок
                IsCreateButtonVisible = false; // Скрыть кнопку "Создать"
               // IsUpdateButtonVisible = true;  // Показать кнопку "Обновить"
                //IsAddButtonVisible = false; // Скрыть кнопку "Добавить"
                //IsDeleteButtonVisible = true; // Показать кнопку "Удалить"
            }
            else
            {
                // Если строка не выбрана, скрываем кнопку "Удалить"
                //IsDeleteButtonVisible = false;
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
        //private bool _isUpdateButtonVisible;
        //public bool IsUpdateButtonVisible
        //{
        //    get => _isUpdateButtonVisible;
        //    set
        //    {
        //        _isUpdateButtonVisible = value;
        //        OnPropertyChanged();
        //    }
        //}

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

     
        //Кнопка Редактировать
        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SaveUpdateButtonText));
                OnPropertyChanged(nameof(SaveUpdateCommand));
            }
        }

        // Текст кнопки в зависимости от режима
        public string SaveUpdateButtonText => IsEditMode ? "Обновить" : "Сохранить";

        // Команда в зависимости от режима
        public ICommand SaveUpdateCommand => IsEditMode ? UpdateCommand : SaveCommand;



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
