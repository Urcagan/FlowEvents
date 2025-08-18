using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories
{
    public class SqliteEventRepository : IEventRepository
    {
        private readonly string _connectionString;
        public SqliteEventRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }


        // Метод для получения событий из базы данных
        public List<EventForView> GetEvents(string queryEvent)
        {
            var eventsDict = new Dictionary<int, EventForView>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(queryEvent, connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int eventId = reader.GetInt32(reader.GetOrdinal("id"));

                        // Если события еще нет в словаре, добавляем его
                        if (!eventsDict.TryGetValue(eventId, out var eventModel))
                        {
                            eventModel = new EventForView
                            {
                                Id = eventId,
                                DateEventString = reader.GetString(reader.GetOrdinal("DateEvent")),
                                Unit = reader.GetString(reader.GetOrdinal("Unit")),
                                OilRefining = reader.IsDBNull(reader.GetOrdinal("OilRefining")) ? null : reader.GetString(reader.GetOrdinal("OilRefining")),
                                Category = reader.GetString(reader.GetOrdinal("Category")),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                                Action = reader.IsDBNull(reader.GetOrdinal("Action")) ? null : reader.GetString(reader.GetOrdinal("Action")),
                                DateCreate = reader.GetString(reader.GetOrdinal("DateCreate")),
                                Creator = reader.GetString(reader.GetOrdinal("Creator"))
                            };
                            eventsDict[eventId] = eventModel;
                        }

                        // Если есть прикрепленный файл, добавляем его
                        if (!reader.IsDBNull(reader.GetOrdinal("FileId")))
                        {
                            eventModel.AttachedFiles.Add(new AttachedFileModel(_connectionString) // Передаем строку подключения
                            {
                                FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
                                FileCategory = reader.GetString(reader.GetOrdinal("FileCategory")),
                                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                                FileType = reader.GetString(reader.GetOrdinal("FileType")),
                                UploadDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("UploadDate")))
                            });
                        }
                    }
                }
            }
            return eventsDict.Values.ToList();
        }


    }
}
