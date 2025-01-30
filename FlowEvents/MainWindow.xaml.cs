using ShiftTaskLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public DatabaseHelper _databaseHelper;

        public ObservableCollection<Item> Items {  get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _databaseHelper = new DatabaseHelper(Global_Var.pathDB);    // Инициализация копии класса работы с БД

            lblPath.Text = "Путь: " + Global_Var.pathDB;

            //Инициализация коллекции с данными
            Items = new ObservableCollection<Item>
            {
                new Item {Name = "Option 1"},
                new Item {Name = "Option 2"},
                new Item {Name = "Option 3"},
                new Item {Name = "Option 2"}
            };
            comboBox.ItemsSource = Items; //Привязка коллекции к ComboBox

            //Подписываемся на событие, чтобы контролировать выпадающий список
            comboBox.DropDownClosed += ComboBox_DropDownClosed;
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            //Оброботка закрытия выпадающего списка
            var selectedItems = Items.Where(item => item.IsSelected).ToList();
            var selectedName = string.Join(", ", selectedItems.Select(item => item.Name));
            //ПРИМЕР ВЫВОДА ЭЛЕМЕНТОВ В ЗАГОЛОВОК ComboBox         
            comboBox.Text=selectedName;
        }

        public class Item
        {
            public string Name { get; set; }
            public bool IsSelected { get; set; }
        }
    }
}
