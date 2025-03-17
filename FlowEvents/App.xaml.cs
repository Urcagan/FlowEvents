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
        private IServiceProvider _serviceProvider;

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
            base.OnStartup(e);

            // Настройка сервисов
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Создание провайдера сервисов
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Запуск главного окна Создание MainWindow через DI
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            AppSettings appSettings = AppSettings.Load(); // Загружаем настройки программы из файла при запуске программы
           
            // Регистрация сервиса базы данных
            services.AddSingleton<IDatabaseService>(provider =>
                new DatabaseService(appSettings.pathDB));

            // Регистрация главного окна
            services.AddTransient<MainWindow>();

            // Регистрация ViewModel
            services.AddTransient<MainViewModel>();
        }
    }
}
