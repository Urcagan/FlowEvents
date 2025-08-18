using FlowEvents.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly IPolicyAuthService _authService;
        private readonly MainViewModel _mainViewModel;


        // Конструктор по умолчанию (без параметров)
        public MainWindow()
        {
            InitializeComponent();

            // Инициализация сервиса (можно вынести в DI-контейнер)
            _authService = new PolicyAuthService();

            // Создаем MainViewModel и передаем сервис
            _mainViewModel = new MainViewModel(_authService);

            DataContext = _mainViewModel;

            // Подписываемся на события загрузки и закрытия окна
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Загружаем настройки ширины столбцов
            LoadDataGridColumnsSettings();

            // Подписываемся на изменения ширины столбцов
            // ВЫКЛЮЧИЛ ДАБЫ НЕ ПЕРЕПИСЫВАТЬ ФАЙЛ НАСТРОЕК МНОЖЕСТВО РАЗ
    //        SubscribeToColumnWidthChanges(); 

            // Получаем ViewModel из DataContext
            if (DataContext is MainViewModel viewModel)
            {
                // Вызываем метод загрузки и проверки данных
                viewModel.StartUP();
            }
        }


        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Сохраняем текущие ширины столбцов
            SaveDataGridColumnsSettings();
        }

        private void LoadDataGridColumnsSettings()
        {
            // Проверяем, есть ли сохраненные настройки
            if (App.Settings.DataGridColumnWidths == null ||
                App.Settings.DataGridColumnWidths.Count == 0)
                return;

            // Применяем настройки для каждого столбца
            foreach (var column in eventsDataGrid.Columns)
            {
                var header = column.Header?.ToString();

                // Если для этого столбца есть сохраненная ширина
                if (!string.IsNullOrEmpty(header) &&
                    App.Settings.DataGridColumnWidths.TryGetValue(header, out var width))
                {
                    // Устанавливаем сохраненную ширину
                    //column.Width = new DataGridLength(width, DataGridLengthUnitType.Pixel);
                    column.Width = width;
                }
            }
        }

        private void SaveDataGridColumnsSettings()
        {
            // Очищаем предыдущие настройки
       //     App.Settings.DataGridColumnWidths.Clear();

            // Сохраняем текущие ширины всех столбцов
            foreach (var column in eventsDataGrid.Columns)
            {
                var header = column.Header?.ToString();
                if (!string.IsNullOrEmpty(header))
                {
                    App.Settings.DataGridColumnWidths[header] = column.ActualWidth;
                }
            }

            // Сохраняем настройки в файл
            App.Settings.SaveSettingsApp();
        }

        private void SubscribeToColumnWidthChanges()
        {
            // Подписываемся на изменение ширины для каждого столбца
            foreach (var column in eventsDataGrid.Columns)
            {
                // Используем DependencyPropertyDescriptor для подписки на изменение свойства
                DependencyPropertyDescriptor
                    .FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn))
                    .AddValueChanged(column, (o, args) =>
                    {
                        // При изменении ширины столбца сразу сохраняем настройки
                        var col = o as DataGridColumn;
                        if (col?.Header != null)
                        {
                            App.Settings.DataGridColumnWidths[col.Header.ToString()] = col.ActualWidth;
                            App.Settings.SaveSettingsApp();
                        }
                    });
            }
        }

    }


}
