using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services;
using FlowEvents.Services.Interface;
using FlowEvents.Settings;
using FlowEvents.Users;
using FlowEvents.ViewModels;
using FlowEvents.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class MainViewModel : INotifyPropertyChanged
    {
        //-------------------------
        //      Зависимости        |
        //-------------------------
        private readonly IPolicyAuthService _authService; // Сервис проверки пользователя
        private readonly IEventRepository _eventRepository; //Сервис работы с Event
        private readonly IDatabaseValidationService _validationService; // Сервис проверки базы данных
        private readonly IUserInfoService _userInfoService; // сервис получения данных пользователя windows

        //-----------------------------------------------------------------------------------

        private bool _isLoading;
        private string _currentDbPath;
        private string _currentUsername;
        private string _userName;
        private List<string> _currentUserPermissions = new List<string>();
        private User _currentUser;  // Кэш пользователя (опционально)
        private DateTime _sartDate = DateTime.Now; // Значение по умолчанию
        private DateTime _endDate = DateTime.Now; // Значение по умолчанию
        private Unit _selectedUnit;
        private bool _isAllEvents;
        private string _queryEvent; // Для хранения Запроса получения Events 
        private EventForView _selectedEvent; // Выбранное событие в таблице
        
        
        private ObservableCollection<Unit> _units { get; set; } = new ObservableCollection<Unit>();


        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public AppSettings appSettings; // Объект параметров приложения
        public string CurrentDbPath
        {
            get => _currentDbPath;
            set
            {
                _currentDbPath = value;
                Global_Var.pathToDB = value;
                Global_Var.ConnectionString = GetConnectionString();
                LoadUserPermissions();
                OnPropertyChanged(nameof(CurrentDbPath));
            }
        }
        public string CurrentUsername
        {
            get => _currentUsername;
            set
            {
                _currentUsername = value;
                LoadCurrentUser();  // Перезагружаем пользователя при смене логина
                                    //  OnPropertyChanged(nameof(CanEditDocument));
                LoadUserPermissions();
            }
        }
        public List<string> CurrentUserPermissions // Публичное свойство для привязки в XAML
        {
            get => _currentUserPermissions;
            private set
            {
                _currentUserPermissions = value;
                OnPropertyChanged(nameof(CurrentUserPermissions));
            }
        }
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
        public Unit SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged();
                UpdateQuery();
            }
        }
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
        public string QueryEvent
        {
            get => _queryEvent;
            set
            {
                _queryEvent = value;
                LoadEvents();
            }
        }
        public EventForView SelectedEvent
        {
            get => _selectedEvent;
            set
            {
                if (_selectedEvent == value)
                    return; // Не обновлять, если значение не изменилось
                _selectedEvent = value;
                OnPropertyChanged(nameof(SelectedEvent));
                OnPropertyChanged(nameof(SelectedEventCollection)); // Критически важно!
                OnPropertyChanged(nameof(MonitoringFiles)); //Фильтрация привязанных файлов по признаку мониторига
                OnPropertyChanged(nameof(DocumentFiles));
            }
        }
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                Global_Var.UserName = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Unit> Units
        {
            get => _units;
            set
            {
                _units = value;
                OnPropertyChanged(nameof(Units)); // Реализуйте INotifyPropertyChanged
            }
        }

        public ObservableCollection<EventForView> Events { get; set; } = new ObservableCollection<EventForView>(); // Коллекция для записей журнала (автоматически уведомляет об изменениях)

          //=======================================================================
        public RelayCommand SettingOpenWindow { get; }
        public RelayCommand UnitOpenWindow { get; }
        public RelayCommand CategoryOpenWindow { get; }
        public RelayCommand EventAddWindow { get; }
        public RelayCommand EditEventCommand { get; } // команда для редактирования
        public RelayCommand DeleteEventCommand { get; }
        public RelayCommand UserManagerWindow { get; } // Позже удалить----------------------------
        public RelayCommand OpenPermissionWindowCommand { get; set; }
        public RelayCommand DownDateCommand { get; }
        public RelayCommand UpDateCommand { get; }
        public RelayCommand CheckUpdateAppCommand { get; } // Кнопка проверки обновления программы 
        public RelayCommand LoginCommand { get; }
        public RelayCommand UserWindowCommand { get; }

        //===============================================================================================================================================

        public MainViewModel(IPolicyAuthService authService, IEventRepository eventRepository , IDatabaseValidationService validationService, IUserInfoService userInfoService)
        {
            _authService = authService;
            _eventRepository = eventRepository;
            _validationService = validationService;
            _userInfoService = userInfoService;

            SettingOpenWindow = new RelayCommand(SettingsMenuItem);
            UnitOpenWindow = new RelayCommand(UnitMenuItem);
            CategoryOpenWindow = new RelayCommand(CategoryMenuItem);
            EventAddWindow = new RelayCommand(AddNewEvent);
            EditEventCommand = new RelayCommand(EditEvent);
            DeleteEventCommand = new RelayCommand(DeletEvent);
            OpenPermissionWindowCommand = new RelayCommand(OpenPermissionWindow);
            CheckUpdateAppCommand = new RelayCommand(CheckUpdateApp);
            LoginCommand = new RelayCommand(Login);
            UserWindowCommand = new RelayCommand(UserInfoWindow); 

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
            //  User();
        }


        //=======================================
        //      ИНИЦИАЛИЗАЦИЯ РАБОТЫ ПРОГРАММЫ   |
        //=======================================

        public async Task StartUPAsync() // ← 6. МЕТОД ИНИЦИАЛИЗАЦИИ Запускается после старта программы. Запуск метода находится в MainWindow.xaml.cs по событию Loaded
        {
            IsLoading = true;
            try
            {
                
                // Способ 1: Полная информация о пользователе windows
                var userInfo = _userInfoService.GetCurrentUserInfo();   // Получает данные о пользователе Windows
              //  userInfo.PrintInfo();

                CurrentUsername = userInfo.DisplayName; //WindowsIdentity.GetCurrent().Name; //Environment.UserName; // Устанавливаем текущего пользователя
                UserName = CurrentUsername; // Отображаем имя пользователя в интерфейсе

                string pathDB = App.Settings.pathDB; 

                // Проверка и загрузка БД в одном методе
                var validationResult = await ValidateAndLoadDatabaseAsync(pathDB);
                if (!validationResult.IsValid)
                {
                    // Если валидация не прошла, выходим из метода
                    return;
                }

                

                CurrentDbPath = pathDB; // Устанавливаем текущий путь к базе данных

                await LoadUnitsToComboBoxAsync(); // Загружаем перечень установок из базы данных
                                                  //
                                                  //_currentUser = _authService.GetUser(_currentDbPath, _currentUsername);
                                                  // _currentUser = _authService.GetUser(pathDB, "User3");

                
                                                        //      CurrentUsername = "Администратор"; // Временно устанавливаем пользователя для тестирования

                //await LoadCategoriesAsync();
                //await LoadUserDataAsync();
                // Другие инициализационные задачи
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<DatabaseValidationResult> ValidateAndLoadDatabaseAsync(string databasePath)
        {
            var result = await _validationService.ValidateDatabaseAsync(databasePath, App.Settings.VerDB);

            if (!result.IsValid)
            {
                await HandleDatabaseValidationErrorAsync(result);
                return result;
            }

            return result;
        }

        //private async Task ValidateAndLoadDatabaseAsync(string databasePath)
        //{
        //    var result = await _validationService.ValidateDatabaseAsync( databasePath, App.Settings.VerDB);

        //    if (!result.IsValid)
        //    {
        //        await HandleDatabaseValidationErrorAsync(result);
        //        return;
        //    }

        //   // await LoadApplicationDataAsync(result.DatabaseInfo);
        //}

        private async Task HandleDatabaseValidationErrorAsync(DatabaseValidationResult result)
        {
            MessageBox.Show(result.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);

            // Можно предложить выбрать новый файл или другие действия
            // НАДО ЗАПРЕТИТЬ ДОСТУП К ИНТЕРФЕЙСУ ПРОГРАММЫ
        }


        private void UpdateQuery()
        {
            QueryEvent = _eventRepository.BuildSQLQueryEvents(SelectedUnit, IsAllEvents ? null : (DateTime?)StartDate, IsAllEvents ? null : (DateTime?)EndDate, IsAllEvents);
        }

        // Метод загрузки прав из БД
        private void LoadUserPermissions()
        {
            if (string.IsNullOrEmpty(_currentDbPath)) return;

            CurrentUserPermissions = _authService.GetUserPermissions(_currentDbPath, _currentUsername);
        }

        
        public IEnumerable<AttachedFileModel> MonitoringFiles =>
                SelectedEvent?.AttachedFiles?
                    .Where(f => f.FileCategory?.Contains("monitoring") == true)
                    ?? Enumerable.Empty<AttachedFileModel>();

        public IEnumerable<AttachedFileModel> DocumentFiles =>
                SelectedEvent?.AttachedFiles?
                    .Where(f => f.FileCategory?.Contains("document") == true)
                    ?? Enumerable.Empty<AttachedFileModel>();

        public ObservableCollection<EventForView> SelectedEventCollection =>
                SelectedEvent == null
                    ? new ObservableCollection<EventForView>()
                    : new ObservableCollection<EventForView> { SelectedEvent };


        //Получаем строку подключения к БД
        private static string GetConnectionString()
        {
            string pathDB = App.Settings.pathDB;
            return $"Data Source={pathDB};Version=3;foreign keys=true;";
        }


        //
        private void LoadCurrentUser() //Загрузка текущего пользователя 
        {
            if (string.IsNullOrEmpty(_currentDbPath) || string.IsNullOrEmpty(_currentUsername))
            {
                _currentUser = null;
                return;
            }
            _currentUser = _authService.GetUser(_currentDbPath, _currentUsername);
        }

        // Загрузкак комбобокса установок
        public async Task  LoadUnitsToComboBoxAsync()
        {
            Units.Clear();
            Units.Insert(0, new Unit { Id = -1, UnitName = "Все" });

            try
            {
                var unitsFromDb = await _eventRepository.GetUnitFromDatabaseAsync();    // Асинхронно загружаем данные из базы

                foreach (var unit in unitsFromDb)   // Добавляем загруженные данные в коллекцию
                {
                    Units.Add(unit);
                }


                SelectedUnit = Units.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }


        // Метод для загрузки данных из базы
        public async Task LoadEvents()
        {
            Events.Clear();
            
            if (QueryEvent == null) return;
            var eventsFromDb = await _eventRepository.GetEventsAsync(QueryEvent);  // Получаем данные из базы

            // Добавляем данные в коллекцию
            foreach (var eventModel in eventsFromDb)
            {
                Events.Add(eventModel);
            }
        }

        private void UserInfoWindow(object parameter)
        {
            var userInfoViewModel = App.ServiceProvider.GetRequiredService<UserInfoViewModel>();                                        // 1. Берем ViewModel из контейнера
            var userInfoWindow = new UserInfoWindow { DataContext= userInfoViewModel};                                                  // 2. Создаем окно обычным способом
       //     userInfoWindow.DataContext = userInfoViewModel;                                           // 3. Связываем ViewModel с окном с установкой контекста
            userInfoWindow.Owner = Application.Current.MainWindow;                                    // 4. Устанавливает главное окно приложения как владельца (owner) для окна                                                                                                        
            userInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;                 // 5. Центрируем относительно владельца если не прописано м xaml (WindowStartupLocation="CenterOwner")
            if (userInfoWindow.ShowDialog() == true) { }                                              // 6. Показываем модально         
        }

        // Вызов окна Объектов
        private void UnitMenuItem()
        {
            var unitViewModel = App.ServiceProvider.GetRequiredService<UnitViewModel>();    // 1. Берем ViewModel из контейнера
            var unitsView = new UnitsView();                                                // 2. Создаем окно обычным способом
            unitsView.DataContext = unitViewModel;   

            async void ClosedHandler(object sender, EventArgs e)                                  // 4. Создает обработчик события Closed, который автоматически отписывается от события после первого срабатывания.
            {
                unitsView.Closed -= ClosedHandler; // Отвязываем
                await LoadUnitsToComboBoxAsync(); // Перезагружаем установки после закрытия окна UnitsView
            }
            unitsView.Closed += ClosedHandler;                                              // 5. Подписываемся на событие Closed окна UnitsView
            
            unitsView.Owner = Application.Current.MainWindow;                               // 6. Устанавливает главное окно приложения как владельца (owner) для окна 
            if (unitsView.ShowDialog() == true) { }                                         // 7. Показываем модально
        }

        // Вызов окна Категорий событий
        private void CategoryMenuItem(object parameter)
        {            
            var categoryViewModel = App.ServiceProvider.GetRequiredService<CategoryViewModel>();    // 1. Берем ViewModel из контейнера
            var categoryView = new CategoryView();                                                  // 2. Создаем окно обычным способом
            categoryView.DataContext = categoryViewModel;                                           // 3. Связываем ViewModel с окном 
            categoryView.Owner = Application.Current.MainWindow;                                    // 4. Устанавливает главное окно приложения как владельца (owner) для окна                                                                                                        
            categoryView.WindowStartupLocation = WindowStartupLocation.CenterOwner;                 // 5. Центрируем относительно владельца если не прописано м xaml (WindowStartupLocation="CenterOwner")
            if (categoryView.ShowDialog() == true) { }                                              // 6. Показываем модально         
        }

        // Вызов окна настроек программы
        private void SettingsMenuItem(object parameter)
        {
            var SettingsViewModel = App.ServiceProvider.GetRequiredService<SettingsViewModel>();    // 1. Берем ViewModel из контейнера
            var SettingsWindow = new SettingsWindow();                                              // 2. Создаем окно обычным способом
            SettingsWindow.DataContext = SettingsViewModel;                                         // 3. Связываем ViewModel с окном 

            async void ClosedHandler(object sender, EventArgs e)                                          // 4. Создает обработчик события Closed, который автоматически отписывается от события после первого срабатывания.
            {
                SettingsWindow.Closed -= ClosedHandler; // Отвязываем
             //   StartUP();
                await StartUPAsync();
            }
            SettingsWindow.Closed += ClosedHandler;                                                 // 6.Подписываемся на событие закрытия окна

            SettingsWindow.Owner = Application.Current.MainWindow;                                  // 7. Устанавливает главное окно приложения как владельца (owner) для окна
            SettingsWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;               // 8. Центрируем относительно владельца если не прописано м xaml (WindowStartupLocation="CenterOwner")
            if (SettingsWindow.ShowDialog() == true) { }                                            // 9. Показываем модально 

        }

        // Вызов окна настроек пользователей и их прав доступа.
        private void OpenPermissionWindow(object parametrs)
        {
            var PermissionViewModel = App.ServiceProvider.GetRequiredService<PermissionViewModel>();    // 1. Берем ViewModel из контейнера
            var PermissionWindow = new PermissionWindow();                                              // 2. Создаем окно обычным способом
            PermissionWindow.DataContext = PermissionViewModel;                                         // 3. Связываем ViewModel с окном 
            PermissionWindow.Owner = Application.Current.MainWindow;                                    // 4. Устанавливает главное окно приложения как владельца (owner) для окна
            PermissionWindow.ShowDialog();                                                              // 5. Показываем модально     
        }

        //private void UserManagerMenuItem(object parameter)
        //{
        //    var UserManagerModel = App.ServiceProvider.GetRequiredService<UserManagerModel>();
        //    var UserManager = new UserManager();
        //    UserManager.DataContext = UserManagerModel;
        //    UserManager.Owner = Application.Current.MainWindow;
        //    if (UserManager.ShowDialog() == true) { }

        //    //var userManagerModel = new UserManagerModel();
        //    //UserManager userManager = new UserManager(userManagerModel);
        //    //if (userManager.ShowDialog() == true) { }
        //}

        private void Login(object parameter)
        {
            LoginUser loginUser = new LoginUser();
            if (loginUser.ShowDialog() == true) { }
        }

        // Вызываем окно для обавления нового события
        private void AddNewEvent(object parameter)
        {
            var eventViewModel = App.ServiceProvider.GetRequiredService<EventViewModel>(); // 1. Берем ViewModel из контейнера
            var eventWindow = new EventWindow();                             // 2. Создаем окно обычным способом
            eventWindow.DataContext = eventViewModel;
            eventWindow.Owner = Application.Current.MainWindow;

            // Создаем обработчик, который отвяжет себя после выполнения
            async void ClosedHandler(object sender, EventArgs e)
            {
                eventWindow.Closed -= ClosedHandler; // Отвязываем
                await LoadEvents();
            }

            eventWindow.Closed += ClosedHandler;
            if (eventWindow.ShowDialog() == true) { }
        }


        private void EditEvent(object parameter)
        {
            if (parameter is EventForView selectedEvent)
            {
                var eventViewModel = ActivatorUtilities.CreateInstance<EventViewModel>(App.ServiceProvider, selectedEvent); // 1. Берем ViewModel из контейнера и передаем в конструктор выбранное событие
                                                                                                                            //  Как это работает:
                                                                                                                            // 1. ActivatorUtilities анализирует конструкторы EventViewModel
                                                                                                                            // 2. Видит, что есть конструктор с параметром EventForView eventToEdit
                                                                                                                            // 3. Передает ваш selectedEvent в этот параметр
                                                                                                                            // 4. Остальные зависимости(если есть) резолвятся из App.ServiceProvider
                var eventWindow = new EventWindow();
                eventWindow.DataContext = eventViewModel;
                eventWindow.Owner = Application.Current.MainWindow;

                // Обработчик закрытия
                async void ClosedHandler(object sender, EventArgs e)
                {
                    eventWindow.Closed -= ClosedHandler; // Отвязываем
                    await LoadEvents();
                }

                eventWindow.Closed += ClosedHandler;
                if (eventWindow.ShowDialog() == true) { }
                ;
            }
        }


        // Удаление события
        private void DeletEvent(object parameter)
        {
            List<AttachedFileForEvent> listAttachedFIles = new List<AttachedFileForEvent>();

            if (parameter is EventForView selectedEvent)
            {
                int eventId = selectedEvent.Id; //Получили ID события которое надо удалить

                var confirm = MessageBox.Show(
                    $"Вы действительно хотите удалить событие от {selectedEvent.DateEvent:dd.MM.yyyy} по объекту {selectedEvent.Unit} ?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                listAttachedFIles = _eventRepository.GetIdFilesOnEvent(eventId);    // 1. Получаем перечень файлов связанных с этим событием

                DeleteFiles(listAttachedFIles); // 2. Удаляем Файлы связанные с событием

                DeleteEventFromDB(eventId); // 3. Удалям записи о событии из БД (таблица Events) Записи о файлах удаляются автоматически каскадом
            }
        }

        // Удаление файлов связанных с событием
        private bool DeleteFiles(List<AttachedFileForEvent> listAttact)
        {
            if (!listAttact.Any()) return true;

            bool allDeleted = true;

            foreach (var file in listAttact)
            {
                if (!DeleteFileFromDisk(file.FilePath))
                    allDeleted = false;
            }
            return allDeleted;
        }

        private bool DeleteFileFromDisk(string path)
        {
            try
            {
                // 1. Удаление файла с диска
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                return false; // файл не существовал
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить файл: {ex.Message}");
                return false;
            }
        }


        private async void CheckForUpdate(string path)
        {
            await Updater.CheckForUpdateAsync(path);//Вызов метода для проверки обновлений
        }

        //Проверка обновления программы
        private void CheckUpdateApp(object parameter)
        {
            MessageBox.Show("Проверка обновлений");

            try
            {
                string repositoryPath = App.Settings.UpdateRepository; //Получаем из конфигурации путь к файлу с параметрами обновления приложения
                CheckForUpdate(repositoryPath); //Проверка обновлений. И выполнение обновления
            }
            catch (Exception ex)
            {
                //Обрабатываем ошибки
                MessageBox.Show("Ошибка: " + ex.Message, "Обработка ошибки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteEventFromDB(int eventId)
        {
            try
            {
                bool success = await _eventRepository.DeleteEventAsync(eventId);

                if (success)
                {
                    RemoveEventById(eventId); // Удаляем из UI только после успешной транзакции
                }
                else
                {
                    MessageBox.Show("Не удалось удалить событие");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}");
            }
        }

        private void RemoveEventById(int id)
        {
            if (Events == null || Events.Count == 0) return;
            var eventToRemove = Events.FirstOrDefault(e => e.Id == id);
            if (eventToRemove != null)
            {
                Events.Remove(eventToRemove);
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
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) // Метод для генерации события
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
