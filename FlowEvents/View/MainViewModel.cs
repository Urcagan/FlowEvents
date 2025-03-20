using FlowEvents.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Data.SQLite;

namespace FlowEvents
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _connectionString; // = $"Data Source={Global_Var.pathDB};Version=3;";
        // Коллекция для хранения данных (автоматически уведомляет об изменениях)
        public ObservableCollection<EventsModel> Events { get; set; } = new ObservableCollection<EventsModel>();

        
        public MainViewModel( ) 
        {
            // загрузка настроек программы из файла конфигурации
 //           appSettings = AppSettings.GetSettingsApp(); // Загружаем настройки программы из файла при запуске программы
 //           string pathDB = appSettings.pathDB;     // получаем путь располажения файла БД

            // Проверяем наличие файла БД по указанному пути 
            //if (!File.Exists(pathDB))
            //{
            //    MessageBox.Show("Файл базы данных не найден. Пожалуйста, укажите новый путь к базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}

            //PathLoad();

            // Проверка наличия файла базы данных перед загрузкой данных
            //if (CheckDatabaseFile())
            //{
            //    LoadEvents();   // Загрузка данных при создании ViewModel
            //}
                       
        }

       

        private bool CheckDatabaseFile()
        {
            if (!File.Exists(Global_Var.pathDB))
            {
                MessageBox.Show("Файл базы данных не найден. Пожалуйста, укажите новый путь к базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }


        // Метод для загрузки данных из базы
        private void LoadEvents()
        {
            appSettings = AppSettings.GetSettingsApp();
            
            // Получаем данные из базы
            var eventsFromDb = GetEvents();

            // Добавляем данные в коллекцию
            foreach (var eventModel in eventsFromDb)
            {
                Events.Add(eventModel);
            }
        }

        public void PathLoad()
        {

            appSettings = AppSettings.GetSettingsApp(); // Загружаем настройки программы из файла при запуске программы
            string pathDB = appSettings.pathDB;
            
            if ( !CheckDB.CheckPathDB(pathDB)) 
                return;  // Проверяем путь к базе данных и выходим, если он неверен

            Global_Var.pathDB = pathDB; //Записываем в глобальную переменную

                databaseHelper = new DatabaseHelper(Global_Var.pathDB);    // Инициализация копии класса работы с БД

                //НЕОБХОДИМО ПЕРЕДЕЛАТЬ ДЛЯ ВЫВОДА ПУТИ В ОКНО
                //lblPath.Text = "Путь: " + Global_Var.pathDB; //Global_Var.pathDB;

                
                // Проверка базы данных на исправность
                if (CheckDB.ALLCheckDB(databaseHelper, Global_Var.pathDB, "Config", appSettings.VerDB))
                {
                    // Создаем и показываем окно настроек
                    //SettingsWindow settingsWindow = new SettingsWindow( databaseHelper );
                    //settingsWindow.ShowDialog(); // Открываем окно как модальное
                    IsCheckDB = true;
                }

            //Execute();

        }

         


        // Загрузка категорий из базы данных
        public List<EventsModel> GetEvents()
        {
            var events = new List<EventsModel>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT id, DateEvent, Unit, Category, Description, Action, DateCreate, Creator  FROM vwEvents", connection);
                using (var reader = command.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("id");
                    int dateIndex = reader.GetOrdinal("DateEvent");
                    int unitIndex = reader.GetOrdinal("Unit");
                    int categotyIndex = reader.GetOrdinal("Category");
                    int descriptionIndex = reader.GetOrdinal("Description");
                    int actionIndex = reader.GetOrdinal("Action");
                    int createIndex = reader.GetOrdinal("DateCreate");
                    int creatorIndex = reader.GetOrdinal("Creator");

                    while (reader.Read())
                    {
                        events.Add(new EventsModel
                        {
                            Id = reader.GetInt32(idIndex),
                            DateEvent = reader.GetString(dateIndex),
                            Unit = reader.GetString(unitIndex),
                            Category = reader.GetString(categotyIndex),
                            Description = reader.IsDBNull(descriptionIndex) ? null : reader.GetString(descriptionIndex),
                            Action = reader.IsDBNull(actionIndex) ? null : reader.GetString(actionIndex),
                            DateCreate = reader.GetString(createIndex),
                            Creator = reader.GetString(creatorIndex)
                        });
                    }
                }
                return events;
            }
        }



        // Вставка нового события в базу данных
        public void AddEvent(EventsModel newEvent)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // SQL-запрос для вставки данных
                var query = @"
                INSERT INTO Events (DateEvent, Unit, Category, Description, Action, DateCreate, Creator)
                VALUES (@DateEvent, @UnitID, @Category, @Description, @Action);";

                using (var command = new SQLiteCommand(query, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@DateEvent", newEvent.DateEvent);
                    command.Parameters.AddWithValue("@Unit", newEvent.Unit);
                    command.Parameters.AddWithValue("@Category", newEvent.Category);
                    command.Parameters.AddWithValue("@Description", newEvent.Description ?? (object)DBNull.Value); // Если Description == null, вставляем NULL
                    command.Parameters.AddWithValue("@Action", newEvent.Action ?? (object)DBNull.Value); // Если Action == null, вставляем NULL
                    command.Parameters.AddWithValue("@DateCtreate", newEvent.DateCreate);
                    command.Parameters.AddWithValue("@Creator", newEvent.Creator);

                    // Выполнение запроса
                    command.ExecuteNonQuery();
                }
            }
        }



        //===============================================================================================================================================
        //Поля, Переменный, Свойства

        private AppSettings appSettings; // Объект параметров приложения
        public DatabaseHelper databaseHelper;
        private bool IsCheckDB = false;





        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для генерации события
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
