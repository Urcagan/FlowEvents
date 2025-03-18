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
    }
}
