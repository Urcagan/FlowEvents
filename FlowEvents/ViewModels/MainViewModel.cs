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
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class MainViewModel : INotifyPropertyChanged
    {

        //-------------------------
        //      Зависимости        |
        //-------------------------
        #region Dependency Injection
        private readonly IPolicyAuthService _authService; // Сервис проверки пользователя
        private readonly IEventRepository _eventRepository; //Сервис работы с Event
        private readonly IDatabaseValidationService _validationService; // Сервис проверки базы данных
        private readonly IUserInfoService _userInfoService; // сервис получения данных пользователя windows
        private readonly IUnitRepository _unitRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAutoRefreshService _autoRefreshService;
        private readonly ICategoryRepository _categoryRepository;
        #endregion
        //-----------------------------------------------------------------------------------
        #region Private Fields
        private bool _isLoading;
        private bool _isBaseValid = false; // Флаг валидности базы данных
        private string _currentDbPath;
        private string _userName;
        private List<string> _currentUserPermissions = new List<string>(); // { "ViewDashboard" };
        private User _currentUser;  // Кэш пользователя (опционально)
        private DateTime _sartDate = DateTime.Now; // Значение по умолчанию
        private DateTime _endDate = DateTime.Now; // Значение по умолчанию
        private Unit _selectedUnit;
        private Category _selectedCategory;
        private bool _isAllEvents;
        private string _queryEvent; // Для хранения Запроса получения Events 
        private EventForView _selectedEvent; // Выбранное событие в таблице
        private UserInfo _currentUserWindows;   //Кеш инфориации о текущем пользователе залогоненым в Windows
        private string _loginUserName; // Кеш имени пользователя для входа
        private bool _isInitializing = true; // Флаг состояния инициализации приложения

        private ObservableCollection<Unit> _units { get; set; } = new ObservableCollection<Unit>();
        #endregion

        #region Public Properties
        public AppSettings appSettings; // Объект параметров приложения

        public bool IsBaseValid
        {
            get => _isBaseValid;
            set
            {
                _isBaseValid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowDatabaseErrorOverlay));
            }
        }
        // Свойство для отображения оверлея с ошибкой БД
        public bool ShowDatabaseErrorOverlay => !IsBaseValid;  // Если база не валидна

        // Свойство для отображения оверлея с доступом
        //public bool ShowAccessOverlay => !CanAccess && IsBaseValid; // Если нет доступа и база валидна
        //Альтернативный метод с отладочной информацией
        public bool ShowAccessOverlay
        {
            get
            {
                bool result = !CanAccess && IsBaseValid;
                System.Diagnostics.Debug.WriteLine($"ShowAccessOverlay: {result}, CanAccess: {CanAccess}, IsBaseValid: {IsBaseValid}");
                return result;
            }
        }

        private string _overlayMessage;
        public string OverlayMessage // Сообщение об ошибке для отображения на оверлее
        {
            get => _overlayMessage;
            set
            {
                _overlayMessage = value;
                OnPropertyChanged();
            }
        }
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        public string CurrentDbPath // Используется только для вывода пути к БД в интерфейс программы
        {
            get => _currentDbPath;
            set
            {
                _currentDbPath = value;
                OnPropertyChanged(nameof(CurrentDbPath));
            }
        }
        public string UserName   // Текущий пользователь в системе
        {
            get => _userName;
            set
            {
                _userName = value;
                Global_Var.UserName = value;
                OnPropertyChanged();
            }
        }

        public List<string> CurrentUserPermissions // Публичное свойство для привязки в XAML
        {
            get => _currentUserPermissions;
            private set
            {
                _currentUserPermissions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAccess));
                OnPropertyChanged(nameof(ShowAccessOverlay));
                System.Diagnostics.Debug.WriteLine($"ShowAccessOverlay из CurrentUserPermissions:  CanAccess: {CanAccess}, IsBaseValid: {IsBaseValid}");
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
                    OnPropertyChanged();
                    _ = LoadEvents();
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
                    OnPropertyChanged();
                    _ = LoadEvents();
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
                _ = LoadEvents();
                Debug.WriteLine("Сработал SelectedUnit");
            }
        }
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                _ = LoadEvents();
                Debug.WriteLine("Сработал SelectedCategory");
            }
        }
        public bool IsAllEvents
        {
            get => _isAllEvents;
            set
            {
                _isAllEvents = value;
                OnPropertyChanged();
                _ = LoadEvents();
                Debug.WriteLine("Сработал IsAllEvents");
            }
        }
        public string QueryEvent
        {
            get => _queryEvent;
            set
            {
                _queryEvent = value;
                _ = LoadEvents();
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

        public ObservableCollection<Unit> Units
        {
            get => _units;
            set
            {
                _units = value;
                OnPropertyChanged(nameof(Units)); // Реализуйте INotifyPropertyChanged
            }
        }

        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>(); // Коллекция для категорий событий
        public ObservableCollection<EventForView> Events { get; set; } = new ObservableCollection<EventForView>(); // Коллекция для записей журнала (автоматически уведомляет об изменениях)

        //public bool CanAccess => _currentUserPermissions?.Contains("ViewDashboard") == true; // Проверка наличия конкретного права ViewDashboard

        //public bool CanAccess // Проверка что есть хоть одно право, кроме GUEST, READONLY, RESTRICTED
        //{
        //    get
        //    {
        //        var excluded = new[] { "GUEST", "READONLY", "RESTRICTED" };
        //        return _currentUserPermissions?.Any(p => !excluded.Contains(p)) == true;
        //    }
        //}

        //public bool CanAccess // Проверка что есть хоть одно из основных прав USER, VIEWER, OPERATOR, MANAGER, ADMIN
        //{
        //    get
        //    {
        //        var basicPermissions = new[] { "USER", "VIEWER", "OPERATOR", "MANAGER", "ADMIN" };
        //        return _currentUserPermissions?.Any(p => basicPermissions.Contains(p)) == true;
        //    }
        //}

        //public bool CanAccess   //
        //{
        //    get
        //    {
        //        // Должен быть не пустой список и содержать хоть что-то кроме гостевых прав
        //        var guestPermissions = new[] { "GUEST", "ANONYMOUS" };
        //        return _currentUserPermissions?.Any() == true &&
        //               _currentUserPermissions.Any(p => !guestPermissions.Contains(p));
        //    }
        //}

        // public bool CanAccess => _currentUserPermissions != null && _currentUserPermissions.Any(); // Проверка что есть хоть одно право, кроме ViewDashboard

        public bool CanAccess
        {
            get
            {
                bool result = CurrentUserPermissions != null && CurrentUserPermissions.Any();
                //Debug.WriteLine("Сработал CanAccess проверка на доступ");
                OnPropertyChanged(nameof(CanImportData));
                OnPropertyChanged(nameof(CanEditEventVisible));
                return result;
            }
        }

        public bool CanImportData
        {
            get
            {
                var basicPermissions = new[] { "ImportData" };
                //Debug.WriteLine("Сработал CanImportData проверка на доступ к импорту данных");
                return _currentUserPermissions?.Any(p => basicPermissions.Contains(p)) == true;
            }
        }

        public bool IsUserLoggedIn => _currentUser != null && !string.IsNullOrEmpty(UserName); // Проверка что пользователь залогинен

        // Поля для автообновления 
        private bool AutoRefreshEnabled => _autoRefreshService.IsEnabled;
        private int AutoRefreshInterval => _autoRefreshService.RefreshInterval;
        public string AutoRefreshStatus => AutoRefreshEnabled ? $"Автообновление каждые {AutoRefreshInterval} сек" : "Автообновление выключено";

        #endregion

        //=======================================================================
        #region RelayCommands
        public RelayCommand SettingOpenWindow { get; }
        public RelayCommand UnitOpenWindow { get; }
        public RelayCommand CategoryOpenWindow { get; }
        public RelayCommand EventAddWindow { get; }
        public RelayCommand EditEventCommand { get; } // команда для редактирования
        public RelayCommand DeleteEventCommand { get; }
        public RelayCommand OpenPermissionWindowCommand { get; set; }
        public RelayCommand DownDateCommand { get; }
        public RelayCommand UpDateCommand { get; }
        public RelayCommand CheckUpdateAppCommand { get; } // Кнопка проверки обновления программы 
        public RelayCommand LoginCommand { get; }
        public RelayCommand LogoutCommand { get; }
        public RelayCommand UserWindowCommand { get; }
        public RelayCommand ToggleAutoRefreshCommand { get; }
        public RelayCommand ManualRefreshCommand { get; }
        public RelayCommand ResetFiltersCommand { get; }
        public RelayCommand ResetTimeCommand { get; }

        public RelayCommand ExportToExcelCommand { get; }

        #endregion

        //===============================================================================================================================================

        public MainViewModel(IPolicyAuthService authService,
                            IEventRepository eventRepository,
                            IDatabaseValidationService validationService,
                            IUserInfoService userInfoService,
                            IUnitRepository unitRepository,
                            IUserRepository userRepository,
                            IAutoRefreshService autoRefreshService,
                            ICategoryRepository categoryRepository)
        {
            _authService = authService;
            _eventRepository = eventRepository;
            _validationService = validationService;
            _userInfoService = userInfoService;
            _unitRepository = unitRepository;
            _userRepository = userRepository;
            _autoRefreshService = autoRefreshService;
            _categoryRepository = categoryRepository;

            SettingOpenWindow = new RelayCommand(SettingsMenuItem);
            //SettingOpenWindow = new RelayCommand(SettingsMenuItem, CanSettings);
            UnitOpenWindow = new RelayCommand(UnitMenuItem, CanEditProduct);
            CategoryOpenWindow = new RelayCommand(CategoryMenuItem, CanEditProduct);
            EventAddWindow = new RelayCommand(AddNewEvent, CanEditEvent);
            EditEventCommand = new RelayCommand(EditEvent, CanEditEvent);
            DeleteEventCommand = new RelayCommand(DeletEvent, CanDeleteEvent);
            OpenPermissionWindowCommand = new RelayCommand(OpenPermissionWindow, CanPermission);
            CheckUpdateAppCommand = new RelayCommand(CheckUpdateApp, CanSettings);
            LoginCommand = new RelayCommand(Login);
            LogoutCommand = new RelayCommand(Logout, CanLogout);
            UserWindowCommand = new RelayCommand(UserInfoWindow);
            DownDateCommand = new RelayCommand((parameter) =>
            {
                _isInitializing = true;
                StartDate = StartDate.AddDays(-1);
                EndDate = EndDate.AddDays(-1);
                _isInitializing = false;
                _ = LoadEvents();
            });
            UpDateCommand = new RelayCommand((parameter) =>
            {
                _isInitializing = true;
                EndDate = EndDate.AddDays(1);
                StartDate = StartDate.AddDays(1);
                _isInitializing = false;
                _ = LoadEvents();
            });

            ManualRefreshCommand = new RelayCommand(async () => await ManualRefresh());

            ResetFiltersCommand = new RelayCommand(() =>
            {
                _isInitializing = true;
                SelectedUnit = Units.FirstOrDefault();
                SelectedCategory = Categories.FirstOrDefault();
                _isInitializing = false;
                _ = LoadEvents();

            });

            ResetTimeCommand = new RelayCommand(() =>
            {
                _isInitializing = true;
                EndDate = DateTime.Now;
                StartDate = DateTime.Now;
                EndDate = DateTime.Now;
                _isInitializing = false;
                _ = LoadEvents();
            });

            ExportToExcelCommand = new RelayCommand(() =>               // Экспотр в Excel
            {
                ExportToExcel exportToExcel = new ExportToExcel(Events);
                exportToExcel.ExportToExcelWithDialog();
                exportToExcel = null;
            });

            // Полная информация о пользователе windows
            _currentUserWindows = _userInfoService.GetCurrentUserInfo();   // Получает данные о пользователе Windows
            UserName = _currentUserWindows.DisplayName; // Получаем имя текущего пользователя
            _loginUserName = _currentUserWindows.Login; // Получаем логин текущего пользователя

            // Подписка на события автообновления
            _autoRefreshService.RefreshRequested += async () => await AutoRefresh();
            _autoRefreshService.OnSettingsChanged += OnAutoRefreshSettingsChanged;

        }



        private void OnAutoRefreshSettingsChanged()
        {
            // Уведомляем об изменении свойств для обновления привязок
            OnPropertyChanged(nameof(AutoRefreshStatus));
        }

        private async Task ManualRefresh()
        {
            await LoadEvents();
        }

        private async Task AutoRefresh()
        {
            if (!CanAccess || IsLoading)
                return;

            await LoadEvents();
            //Debug.WriteLine($"Авто обновление сработало {DateTime.Now} ");
        }


        //==========================================================
        //      Проверка доступности прав пользователя к командам   |
        //==========================================================
        #region Can Permissiv
        private bool CanPermission(object parameter)
        {
            var requiredPermissions = new[] { "ManageUsers", "ConfigureSystem" };
            return _currentUserPermissions?.Any(p => requiredPermissions.Contains(p)) == true;
        }
        private bool CanSettings(object parameter)
        {
            var requiredPermissions = new[] { "ViewDashboard", "ConfigureSystem" };
            return _currentUserPermissions?.Any(p => requiredPermissions.Contains(p)) == true;
        }
        private bool CanEditProduct(object parameter)
        {
            var requiredPermissions = new[] { "ManageProduct", "ConfigureSystem" };
            return _currentUserPermissions?.Any(p => requiredPermissions.Contains(p)) == true;
        }

        public bool CanEditEventVisible => CanEditEvent(null); // Свойство для UI для пользователей которые могут редактировать
        private bool CanEditEvent(object parameter)
        {
            var requiredPermissions = new[] { "EditDocument", "ManageProduct", "ConfigureSystem" };
            return _currentUserPermissions?.Any(p => requiredPermissions.Contains(p)) == true;
        }


        private bool CanDeleteEvent(object parameter)
        {
            var requiredPermissions = new[] { "DeleteDocument", "ManageProduct", "ConfigureSystem" };
            return _currentUserPermissions?.Any(p => requiredPermissions.Contains(p)) == true;
        }

        private bool CanLogout(object parameter) => IsUserLoggedIn; // Проверка возможности выхода
                                                                    //private bool CanAccess(object parameter)
                                                                    //{
                                                                    //    var excludedPermissions = new[] { "ViewDashboard" };
                                                                    //    return _currentUserPermissions?.Any(p => !excludedPermissions.Contains(p)) == true;
                                                                    //}

        private bool CanLogin(object parameter)  // Проверка возможности входа
        {
            bool result = !IsUserLoggedIn && IsBaseValid;
            System.Diagnostics.Debug.WriteLine($"Возможность зологинится : {result}, Пользователь вошол: {IsUserLoggedIn}, База валидна : {IsBaseValid}");
            return result;
        }
        #endregion


        //=======================================
        //      ИНИЦИАЛИЗАЦИЯ РАБОТЫ ПРОГРАММЫ   |
        //=======================================

        public async Task StartUPAsync() // ← 6. МЕТОД ИНИЦИАЛИЗАЦИИ Запускается после старта программы. Запуск метода находится в MainWindow.xaml.cs по событию Loaded
        {
            IsLoading = true;
            try
            {
                string pathDB = App.Settings.pathDB;
                //Debug.WriteLine($"Полученный путь из файла конфигурации: {pathDB}");

                var validationResult = await ValidateAndLoadDatabaseAsync(pathDB); // Проверка и загрузка БД в одном методе
                IsBaseValid = validationResult.IsValid;
                if (!validationResult.IsValid)
                {
                    return; // Если валидация не прошла, выходим из метода
                }

                await GetCurrentUserFromDB(_loginUserName);  //Загрузка информауию о текущем пользователе из БД 
                OnPropertyChanged(nameof(ShowAccessOverlay));

                CurrentDbPath = pathDB; // Отображаем текущий путь к базе данных в итерфейсе программы

                await LoadUnitsToComboBoxAsync(); // Загружаем перечень установок из базы данных
                await LoadCategoriesToComboBoxAsync(); // Загружаем перечень категорий из базы данных


                // Загружаем настройки авто обновления данных Events в сервис
                _autoRefreshService.IsEnabled = App.Settings.AutoRefreshEnabled;        // состояние авто обновления
                _autoRefreshService.RefreshInterval = App.Settings.AutoRefreshInterval; // время автообновления

                _isInitializing = false; // По щкончанию инициализации снимаем флаг

                _ = LoadEvents();  //Далее загружаем данные

                // Другие инициализационные задачи

            }
            finally
            {
                IsLoading = false;
            }
        }


        // Метод для выхода пользователя
        private async void Logout(object parameter)
        {
            Events.Clear();         // Очищаем данные
            SelectedEvent = null;

            // Сбрасываем данные пользователя
            _currentUser = null;
            UserName = string.Empty;
            CurrentUserPermissions?.Clear();

            // Полная информация о пользователе windows
            _currentUserWindows = _userInfoService.GetCurrentUserInfo();   // Получает данные о пользователе Windows
            UserName = _currentUserWindows.DisplayName; // Получаем имя текущего пользователя
            _loginUserName = _currentUserWindows.Login;
            _ = GetCurrentUserFromDB(_loginUserName);  //Загрузка информауию о текущем пользователе из БД 

            OnPropertyChanged(nameof(CurrentUserPermissions));
            OnPropertyChanged(nameof(CanAccess));
            OnPropertyChanged(nameof(IsUserLoggedIn));
            OnPropertyChanged(nameof(ShowAccessOverlay));
            await LoadEvents();
        }


        private async Task<DatabaseValidationResult> ValidateAndLoadDatabaseAsync(string databasePath)
        {
            var result = await _validationService.ValidateDatabaseAsync(databasePath, App.Settings.VerDB);

            if (!result.IsValid)
            {
                HandleDatabaseValidationErrorAsync(result);
                return result;
            }
            return result;
        }


        private void HandleDatabaseValidationErrorAsync(DatabaseValidationResult result)
        {
            OverlayMessage = result.Message;

            //MessageBox.Show(result.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            // Можно предложить выбрать новый файл или другие действия
            // НАДО ЗАПРЕТИТЬ ДОСТУП К ИНТЕРФЕЙСУ ПРОГРАММЫ
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


        private async Task GetCurrentUserFromDB(string Login) //Загрузка данных о текущем пользователе из БД 
        {
            if (string.IsNullOrEmpty(Login))    // Проверка на пустое имя пользователя
            {
                OverlayMessage = "Имя ползователя не определено";
                return;
            }

            bool UserExists = await _userRepository.UserExistsAsync(Login); //Проверяем пользователя на наличие его в базе
            if (!UserExists) // Если пользователя нет в базе, то выходим из метода
            {
                _currentUser = null;
                OverlayMessage = $"Пользователя {Login} нет в перечне допущенных пользователей";
                return;
            }
            _currentUser = _authService.GetUser(Login); // Получаем данные пользователя и помещаем их в пересеную используемую как кеш днных
            CurrentUserPermissions = _authService.GetUserPermissions(Login);

            //OnPropertyChanged(nameof(CanEditEventVisible));
        }

        // Загрузкак комбобокса установок
        public async Task LoadUnitsToComboBoxAsync()
        {
            Units.Clear();
            Units.Insert(0, new Unit { Id = -1, UnitName = "Все объекты" });

            try
            {
                var unitsFromDb = await _unitRepository.GetAllUnitsAsync();    // Асинхронно загружаем данные из базы

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

        // Загрузка комбобокса категорий
        public async Task LoadCategoriesToComboBoxAsync()
        {
            Categories.Clear();
            Categories.Add(new Category { Id = -1, Name = "Все события" });
            try
            {
                // 1. Получение перечня категорий для комбобокса из Category
                var categories = await _categoryRepository.GetAllCategoriesAsync();
                foreach (var category in categories) // Добавляем категории из БД в комбобокс категорий
                {
                    Categories.Add(category);
                }
            }
            catch (Exception)
            {
            }
            SelectedCategory = Categories.FirstOrDefault(); // Установка категории по умолчанию
        }

        private int CountLoad = 0;
        // Метод для загрузки данных из базы       
        public async Task LoadEvents()
        {
            if (_isInitializing) return; // Проверка состояние начальной инициализации приложения

            if (!CanAccess)
                return;
            Events.Clear();

            (string query, List<SQLiteParameter> parameters) = _eventRepository.BuildSQLQueryParametersEvents(SelectedUnit, SelectedCategory, IsAllEvents ? null : (DateTime?)StartDate, IsAllEvents ? null : (DateTime?)EndDate, IsAllEvents);

            if (string.IsNullOrEmpty(query)) return;
            var eventsFromDb = await _eventRepository.GetEventsParamsAsync(query, parameters);

            // Добавляем данные в коллекцию
            foreach (var eventModel in eventsFromDb)
            {
                Events.Add(eventModel);
            }
            CountLoad++;
            Debug.WriteLine($"Сработал LoadEvents {CountLoad} раз");

        }

        private void UserInfoWindow(object parameter)
        {
            var userInfoViewModel = App.ServiceProvider.GetRequiredService<UserInfoViewModel>();                                        // 1. Берем ViewModel из контейнера
            var userInfoWindow = new UserInfoWindow { DataContext = userInfoViewModel };                                                  // 2. Создаем окно обычным способом
                                                                                                                                          //     userInfoWindow.DataContext = userInfoViewModel;                                           // 3. Связываем ViewModel с окном с установкой контекста
            userInfoWindow.Owner = Application.Current.MainWindow;                                    // 4. Устанавливает главное окно приложения как владельца (owner) для окна                                                                                                        
            userInfoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;                 // 5. Центрируем относительно владельца если не прописано м xaml (WindowStartupLocation="CenterOwner")
            if (userInfoWindow.ShowDialog() == true) { }                                              // 6. Показываем модально         
        }

        // Вызов окна Объектов
        private void UnitMenuItem(object parameter)
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


        private async void Login(object parameter)
        {
            var loginViewModel = App.ServiceProvider.GetRequiredService<LoginViewModel>();
            var loginUser = new LoginUser();
            loginUser.DataContext = loginViewModel;
            loginUser.Owner = Application.Current.MainWindow;

            // Подписываемся на закрытие окна
            loginViewModel.CloseAction = () =>
            {
                loginUser.DialogResult = true; // Всегда true, т.к. при ошибках окно не закрывается
            };
            // Показываем окно входа
            if (loginUser.ShowDialog() == true)
            {
                // Получаем аутентифицированного пользователя из ViewModel
                User authenticatedUser = loginViewModel.CurrentUser;

                if (authenticatedUser != null && authenticatedUser.IsAuthenticated)
                {
                    // Используем пользователя в основном окне
                    UserName = authenticatedUser.DisplayName;
                    _loginUserName = authenticatedUser.UserName;
                    _ = GetCurrentUserFromDB(_loginUserName);  //Загрузка информауию о текущем пользователе из БД 

                    // Уведомляем об изменениях
                    OnPropertyChanged(nameof(IsUserLoggedIn));
                    OnPropertyChanged(nameof(CanAccess));
                    OnPropertyChanged(nameof(ShowAccessOverlay));
                    // Другие действия с пользователем...

                    await LoadEvents();
                }
            }
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


        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) // Метод для генерации события
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
