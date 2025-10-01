using FlowEvents.Models;
using FlowEvents.Repositories;
using FlowEvents.Repositories.Implementations;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services;
using FlowEvents.Services.Implementations;
using FlowEvents.Services.Interface;
using FlowEvents.Settings;
using FlowEvents.Users;
using FlowEvents.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static AppSettings Settings { get; private set; } // Поле для Настроек прогаммы 

        //=================================================
        // 1. С ЭТОГО МЕТОДА НАЧИНАЕТСЯ СТАРТ ПРОГРАММЫ
        //=================================================
        protected override void OnStartup(StartupEventArgs e)// Переопределяем метод OnStartup, Внем задаем наш формат
        {
            try
            {

                base.OnStartup(e);      // ← 1. БАЗОВАЯ ИНИЦИАЛИЗАЦИЯ
                                        // Загружает ресурсы App.xaml, инициализирует приложение
                                        // Важно: без этого не загрузятся ресурсы App.xaml!

                // Загружаем настройки при старте приложения
                Settings = AppSettings.GetSettingsApp();

                // Настройка культуры
                ConfigureCulture();

                // Загрузка конфигурации БД (должна быть выполнена до настройки сервисов)
                LoadDatabaseConfiguration();

                // Настройка DI контейнера
                ConfigureServices();

                // Запуск главного окна
                // Получение сервисов из DI-контейнера
                var mainWindow = ServiceProvider.GetService<MainWindow>();                                      // ← 2. СОЗДАНИЕ ОКНА
                if (mainWindow == null) throw new InvalidOperationException("MainWindow not registered");

                var mainViewModel = ServiceProvider.GetService<MainViewModel>();                                // ← 3. СОЗДАНИЕ ViewModel
                if (mainViewModel == null) throw new InvalidOperationException("MainViewModel not registered");


                mainWindow.DataContext = mainViewModel;     // ← 4. ПРИВЯЗКА ДАННЫХ.  Связывание DataContext

                mainWindow.Show();                          // ← 5. ПОКАЗ ОКНА        Отображение главного окна
                                                            // ГДЕ-ТО ПОСЛЕ ЭТОГО ВЫЗЫВАЕТСЯ: mainViewModel.StartUPAsync();
                                                            // Который подписан на событие Loaded в MainWindow.xaml.cs
                                                            // То есть посде гарантированной прогрузки всего UI по событию Loaded,
                                                            // происходит первоначальная загрузка всех остальных параметров и данных программы. 

                //   -------------------------------------------------
                //  |   Полная последовательность запуска программы   |
                //  |   1.Application.Startup()                       |
                //  |   2.App.OnStartup()                             |
                //  |   3. base.OnStartup() ← ресурсы App.xaml        |
                //  |   4.DI - контейнер: создание MainWindow         |
                //  |   5.DI - контейнер: создание MainViewModel      |
                //  |   6.mainWindow.DataContext = mainViewModel      |
                //  |   7.mainWindow.Show() ← окно появляется         |
                //  |   8.MainWindow.Loaded event                     |
                //  |   9. MainViewModel.StartUP() ← ваш метод        |
                //  |   10. Application running                       |
                //   -------------------------------------------------

            }
            catch (Exception ex)
            {
                // Логирование ошибки запуска
                MessageBox.Show($"Ошибка запуска: {ex.Message}");
                Shutdown();
            }

        }

        private void LoadDatabaseConfiguration()
        {
            // Здесь должен быть код загрузки конфигурации
            Global_Var.pathToDB = App.Settings.pathDB;
            Global_Var.ConnectionString = GetConnectionString();
        }

        //-------------------------------------------------------------------------------------------------------------------------
        // КОНФИГУРАЦИЯ СЕРВИСОВ 
        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Регистрируем настройки как Singleton
            services.AddSingleton<AppSettings>(provider => Settings);

            // Провайдер строки подключения (Singleton - один на все приложение)
            // Регистрируем провайдер строки подключения
            services.AddSingleton<IConnectionStringProvider>(provider =>
            {
                var appSettings = provider.GetService<AppSettings>();
                return new ConnectionStringProvider($"Data Source={appSettings.pathDB};Version=3;foreign keys=true;");
            });

            services.AddSingleton<IDatabaseInfoRepository>(provider => new DatabaseInfoRepository());


            // Регистрация сервисов
            
            services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();
            services.AddScoped<IDomainSettingsService, DomainSettingsService>();
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IUserInfoService, UserInfoService>();
            services.AddSingleton<IRoleServices, RoleServices>();
            services.AddSingleton<IPermissionService, PermissionService>();

            // Регистрация репозиториев
            services.AddSingleton<IEventRepository, EventRepository>();
            services.AddSingleton<ICategoryRepository, CategoryRepository>();
            services.AddSingleton<IPolicyAuthService, PolicyAuthService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddSingleton<IDatabaseValidationService, DatabaseValidationService>();
            services.AddSingleton<IPermissionRepository, PermissionRepository>();
            services.AddSingleton<IRoleRepository, RoleRepository>();
            services.AddSingleton<IUnitRepository, UnitRepository>();
            services.AddSingleton<IUserRepository, UserRepository>();




            // Регистрация ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<PermissionViewModel>();
            services.AddTransient<CategoryViewModel>();
            services.AddTransient<UnitViewModel>();
            services.AddTransient<AddUserViewModel>();
            services.AddTransient<UserDomainSearchViewModel>();
            services.AddTransient<UserInfoViewModel>();
            services.AddTransient<EventViewModel>();

            // Регистрация окон
            services.AddTransient<MainWindow>();
            services.AddTransient<PermissionWindow>();

            // Построение провайдера услуг
            ServiceProvider = services.BuildServiceProvider();
        }
        //-------------------------------------------------------------------------------------------------------------------------

        private void ConfigureCulture()
        {
            var cultureInfo = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        //Получаем строку подключения к БД
        private static string GetConnectionString()
        {
            string pathDB = App.Settings.pathDB;
            return $"Data Source={pathDB};Version=3;foreign keys=true;";
        }

    }
}
