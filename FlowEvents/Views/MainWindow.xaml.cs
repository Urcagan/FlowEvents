using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {       
        // Конструктор по умолчанию (без параметров)
        public MainWindow()
        {
            InitializeComponent();

            // Установка заголовка окна программы с версией
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Title = $"Журнал событий v{version.Major}.{version.Minor}.{version.Build}";
            // this.Title = $"Журнал событий v{version}";

            // Подписываемся на события загрузки и закрытия окна
            Loaded += MainWindow_Loaded;

            // Подписка на метод продолжающий иниициализацию программы при старте 
            Loaded += async (s, e) =>
            {
                if (DataContext is MainViewModel viewModel)
                {
                    await viewModel.StartUPAsync();
                }
            };

            Closing += MainWindow_Closing;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {            
            LoadDataGridColumnsSettings(); // Загружаем настройки ширины столбцов

            LoadWindowSettings();           // Загружаем настройки полажения и размера окна программы
        }


        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Сохраняем текущие ширины столбцов
            SaveDataGridColumnsSettings();

            SaveWindowSettings(); // Подготовка к сохранению данных о полажении и размере ркна программы

            // Сохраняем настройки в файл
            //App.Settings.SaveSettingsApp();
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

        private void LoadWindowSettings()
        {
            var mainWindow = Application.Current.MainWindow;

            if (mainWindow != null && App.Settings != null)
            {
                // Проверяем, чтобы окно не выходило за пределы экрана
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var screenHeight = SystemParameters.PrimaryScreenHeight;

                // Восстанавливаем размеры с проверкой границ
                if (App.Settings.WindowWidth > 0 && App.Settings.WindowWidth <= screenWidth)
                    mainWindow.Width = App.Settings.WindowWidth;

                if (App.Settings.WindowHeight > 0 && App.Settings.WindowHeight <= screenHeight)
                    mainWindow.Height = App.Settings.WindowHeight;

                // Восстанавливаем положение с проверкой границ
                if (App.Settings.WindowLeft >= 0 && App.Settings.WindowLeft <= screenWidth - mainWindow.Width)
                    mainWindow.Left = App.Settings.WindowLeft;

                if (App.Settings.WindowTop >= 0 && App.Settings.WindowTop <= screenHeight - mainWindow.Height)
                    mainWindow.Top = App.Settings.WindowTop;

                // Восстанавливаем состояние окна
                if (Enum.TryParse<WindowState>(App.Settings.WindowState, out var windowState))
                {
                    mainWindow.WindowState = windowState;
                }

                System.Diagnostics.Debug.WriteLine($"[WINDOW] Settings loaded: {mainWindow.Left}, {mainWindow.Top}, {mainWindow.Width}x{mainWindow.Height}, State: {mainWindow.WindowState}");
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

            
        }
        private void SaveWindowSettings()
        {
            var mainWindow = Application.Current.MainWindow;

            if (mainWindow != null && mainWindow.WindowState != WindowState.Minimized && mainWindow.IsVisible)
            {
                // Сохраняем только если окно в нормальном состоянии или развернуто
                if (mainWindow.WindowState == WindowState.Normal || mainWindow.WindowState == WindowState.Maximized)
                {
                    App.Settings.WindowLeft = mainWindow.Left;
                    App.Settings.WindowTop = mainWindow.Top;
                    App.Settings.WindowWidth = mainWindow.Width;
                    App.Settings.WindowHeight = mainWindow.Height;
                }
                App.Settings.WindowState = mainWindow.WindowState.ToString();

                //System.Diagnostics.Debug.WriteLine($"[WINDOW] Settings saved: {mainWindow.Left}, {mainWindow.Top}, {mainWindow.Width}x{mainWindow.Height}, State: {mainWindow.WindowState}");
            }
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
