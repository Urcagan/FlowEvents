using FlowEvents.Repositories;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services;
using FlowEvents.Users;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        // Переопределяем метод OnStartup, Внем задаем наш формат
        protected override void OnStartup(StartupEventArgs e)
        {
            var cultureInfo = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            // Загрузка конфигурации БД (должна быть выполнена до настройки сервисов)
            LoadDatabaseConfiguration();

            // Настройка DI контейнера
            ConfigureServices();


            base.OnStartup(e);

            // Запуск главного окна
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void LoadDatabaseConfiguration()
        {
            // Здесь должен быть код загрузки конфигурации из файла
            // Например:
            // Global_Var.ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            // Или загрузка из JSON/XML файла


            Global_Var.pathToDB = App.Settings.pathDB;
            Global_Var.ConnectionString = GetConnectionString();
        }


        //private void ConfigureServices(IServiceCollection services)
        //{
        //    string connectionString = Global_Var.ConnectionString;

        //    // Репозитории
        //    services.AddSingleton<IUserRepository>(provider => new UserRepository(connectionString));
        //    //services.AddSingleton<IRoleRepository>(provider => new RoleRepository(connectionString));

        //    // Сервисы
        //    services.AddSingleton<IDatabaseService, DatabaseService>();

        //    // ViewModel и View
        //    services.AddSingleton<MainViewModel>();
        //    services.AddSingleton<MainWindow>();
        //}

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Регистрация репозиториев с использованием Global_Var.ConnectionString
            services.AddSingleton<IUserRepository>(provider =>
                new UserRepository(Global_Var.ConnectionString));



            // Регистрация сервисов
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IPolicyAuthService, PolicyAuthService>();



            // Регистрация ViewModel
            //services.AddSingleton<MainViewModel>();

            // Регистрация View
            //services.AddSingleton<MainWindow>();

            // Регистрация ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<UserManagerModel>();
            services.AddTransient<PermissionViewModel>();

            // Регистрация окон
            services.AddTransient<MainWindow>();
            services.AddTransient<UserManager>();
            services.AddTransient<PermissionWindow>();

            // Построение провайдера услуг
            ServiceProvider = services.BuildServiceProvider();
        }

        //Получаем строку подключения к БД
        private static string GetConnectionString()
        {
            string pathDB = App.Settings.pathDB;
            return $"Data Source={pathDB};Version=3;foreign keys=true;";
        }

    }
}
