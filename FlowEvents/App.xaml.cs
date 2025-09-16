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

        // Переопределяем метод OnStartup, Внем задаем наш формат
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e); // Важно: без этого не загрузятся ресурсы App.xaml!

                // Настройка культуры
                ConfigureCulture();

                // Загрузка конфигурации БД (должна быть выполнена до настройки сервисов)
                LoadDatabaseConfiguration();

                // Настройка DI контейнера
                ConfigureServices();

                // Запуск главного окна
                                        //var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                var mainWindow = ServiceProvider.GetService<MainWindow>();
                if (mainWindow == null) throw new InvalidOperationException("MainWindow not registered");

                var mainViewModel = ServiceProvider.GetService<MainViewModel>();
                if (mainViewModel == null) throw new InvalidOperationException("MainViewModel not registered");

                mainWindow.DataContext = mainViewModel;
                
                mainWindow.Show();
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
            
            

            // Регистрация сервисов
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IPolicyAuthService, PolicyAuthService>();
            
            

            // Регистрация ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<UserManagerModel>();
            services.AddTransient<PermissionViewModel>();

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
