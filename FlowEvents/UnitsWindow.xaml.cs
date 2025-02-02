using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
using System.Windows.Shapes;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для UnitsWindow.xaml
    /// </summary>
    public partial class UnitsWindow : Window
    {

        private DatabaseHelper _databaseHelper;

        public UnitsWindow(DatabaseHelper databaseHelper)
        {
            InitializeComponent();
            _databaseHelper = databaseHelper; // Сохраняем переданный объект

            LoadUnits(); // Загружаем данные из БД
        }

        public class UnitModel
        {
            public int Id { get; set; }
            public string Unit { get; set; }
            public string Description { get; set; }
        }

        private void LoadUnits()
        {
            try
            {
                DataTable dataTable = _databaseHelper.GetDataTable("SELECT id, Unit, Description FROM Units");

                List<UnitModel> units = new List<UnitModel>();

                foreach (DataRow row in dataTable.Rows)
                {
                    units.Add(new UnitModel
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Unit = row["Unit"].ToString(),
                        Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : string.Empty
                    });
                }

                UnitsListView.ItemsSource = units; // Отображаем данные в ListView
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //  Добавление записи
        private void AddUnit(object sender, RoutedEventArgs e)
        {
            string unitName = UnitTextBox.Text;
            string description = DescriptionTextBox.Text;

            // Проверка, что поле Unit не пустое
            if (string.IsNullOrWhiteSpace(unitName))
            {
                MessageBox.Show("Название unit обязательно для ввода!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_databaseHelper.isRecordPresent("Units", "Unit", unitName))
            {
                MessageBox.Show($"Запись с названием '{unitName}' уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

             _databaseHelper.InsertData($"INSERT INTO Units (Unit, Description) VALUES ('{unitName}', '{description}')");
            /**
                           string query = "INSERT INTO Units (Unit, Description) VALUES (@Unit, @Description)";
                           using (SQLiteCommand command = new SQLiteCommand(query, _databaseHelper.Connection))
                           {
                               command.Parameters.AddWithValue("@Unit", unitName);
                               command.Parameters.AddWithValue("@Description", description);
                               _databaseHelper.ExecuteNonQuery(command);
                           }
            **/
            UnitTextBox.Clear();
            DescriptionTextBox.Clear();
            LoadUnits(); // Обновить данные в ListView

        }

        // Обработка события нажатия на строке таблицы 

        private string _originalUnit;
        private string _originalDescription;
        private void UnitsListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (UnitsListView.SelectedItem is UnitModel selectedUnit)
            {
                // Сохраняем исходные значения
                _originalUnit = selectedUnit.Unit;
                _originalDescription = selectedUnit.Description;

                UnitTextBox.Text = selectedUnit.Unit;
                DescriptionTextBox.Text = selectedUnit.Description;
            }
        }

        // Сохранение изменени
        private void SaveChanges(object sender, RoutedEventArgs e)
        {
            if (UnitsListView.SelectedItem is UnitModel selectedUnit)
            {
                string unitName = UnitTextBox.Text;
                string description = DescriptionTextBox.Text;

                // Проверка на изменения
                if (unitName == _originalUnit && description == _originalDescription)
                {
                    MessageBox.Show("Нет изменений для сохранения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return; // Прерываем выполнение, если изменений нет
                }

                _databaseHelper.UpdateData($"UPDATE Units SET Unit = '{unitName}', Description = '{description}' Where id = '{selectedUnit.Id}'");

                UnitTextBox.Clear();
                DescriptionTextBox.Clear();
                LoadUnits(); // Обновляем данные в ListView
            }
        }
         // Удаление записи
        private void DeleteUnit(object sender, RoutedEventArgs e)
        {
            if (UnitsListView.SelectedItem is UnitModel selectedUnit)
            {
                MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    string query = $"DELETE FROM Units WHERE id = '{selectedUnit.Id}'";
                    _databaseHelper.ExecuteTransaction(query);

                    UnitTextBox.Clear();
                    DescriptionTextBox.Clear();
                    LoadUnits(); // Обновить ListView
                }
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
