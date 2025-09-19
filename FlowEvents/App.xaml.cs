using FlowEvents.Repositories;
using FlowEvents.Repositories.Implementations;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services;
using FlowEvents.Settings;
using FlowEvents.Users;
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
            catch (Exception ex )
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

            // Регистрация репозиториев с использованием Global_Var.ConnectionString
            services.AddSingleton<IPermissionRepository>(provider => new PermissionRepository(Global_Var.ConnectionString));
            services.AddSingleton<IRoleRepository>(provider => new RoleRepository(Global_Var.ConnectionString));
            services.AddSingleton<IUserRepository>(provider => new UserRepository(Global_Var.ConnectionString));
            services.AddSingleton<IEventRepository>(provider => new  EventRepository(Global_Var.ConnectionString));
            services.AddSingleton<ICategoryRepository>(provider => new CategoryRepository(Global_Var.ConnectionString));
            services.AddSingleton<IUnitRepository>(provider => new  UnitRepository(Global_Var.ConnectionString));


            // Регистрация сервисов
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IPolicyAuthService, PolicyAuthService>();
            
            

            // Регистрация ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<UserManagerModel>();
            services.AddTransient<PermissionViewModel>();            
            services.AddTransient<CategoryViewModel>();
            services.AddTransient<UnitViewModel>();
            services.AddTransient<AddUserViewModel>();

            // Регистрация окон
            services.AddTransient<MainWindow>();
            services.AddTransient<UserManager>();
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
