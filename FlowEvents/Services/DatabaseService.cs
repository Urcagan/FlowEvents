using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;

namespace FlowEvents
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = $"Data Source={connectionString};Version=3;";

        }


        // Загрузка категорий из базы данных
        public List<EventsModel> GetEvents()
        {
            var events = new List<EventsModel>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT id, DateEvent, UnitID, Category, Description, Action FROM Events", connection);
                using (var reader = command.ExecuteReader())
                {
                    int idIndex = reader.GetOrdinal("id");
                    int dateIndex = reader.GetOrdinal("DateEvent");
                    int unitIndex = reader.GetOrdinal("UnitID");
                    int categotyIndex = reader.GetOrdinal("Category");
                    int descriptionIndex = reader.GetOrdinal("Description");
                    int actionIndex = reader.GetOrdinal("Action");

                    while (reader.Read())
                    {
                        events.Add(new EventsModel
                        {
                            Id = reader.GetInt32(idIndex),
                            DateEvent = reader.GetString(dateIndex),
                            UnitID = reader.GetInt32(unitIndex),
                            Category = reader.GetInt32(categotyIndex),
                            Description = reader.IsDBNull(descriptionIndex) ? null : reader.GetString(descriptionIndex),
                            Action = reader.IsDBNull(actionIndex) ? null : reader.GetString(actionIndex)
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
                INSERT INTO Events (DateEvent, UnitID, Category, Description, Action)
                VALUES (@DateEvent, @UnitID, @Category, @Description, @Action);";

                using (var command = new SQLiteCommand(query, connection))
                {
                    // Добавление параметров
                    command.Parameters.AddWithValue("@DateEvent", newEvent.DateEvent);
                    command.Parameters.AddWithValue("@UnitID", newEvent.UnitID);
                    command.Parameters.AddWithValue("@Category", newEvent.Category);
                    command.Parameters.AddWithValue("@Description", newEvent.Description ?? (object)DBNull.Value); // Если Description == null, вставляем NULL
                    command.Parameters.AddWithValue("@Action", newEvent.Action ?? (object)DBNull.Value); // Если Action == null, вставляем NULL

                    // Выполнение запроса
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
