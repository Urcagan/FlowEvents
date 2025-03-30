using System.Windows;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // public DatabaseHelper databaseHelper;

        // Конструктор по умолчанию (без параметров)
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            // Подписываемся на событие загрузки окна
            Loaded += MainWindow_Loaded;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Получаем ViewModel из DataContext
            if (DataContext is MainViewModel viewModel)
            {
                // Вызываем метод загрузки и проверки данных
                viewModel.StartUP();
            }
        }

    }


}
