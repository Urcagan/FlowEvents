using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace FlowEvents.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly IConnectionStringProvider _connectionProvider;
        public EventRepository(IConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }



        // Добавдяем событие и связь между событием и объектами
        public async Task<long> AddEventWithUnitsAsync(Event newEvent, IEnumerable<int> unitIds)
        {
            var connectionString = _connectionProvider.GetConnectionString();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Вставляем событие
                var eventId = await InsertEventAsync(connection, transaction, newEvent);

                // 2. Вставляем связи с установками
                if (unitIds?.Any() == true)
                {
                    await InsertEventUnitsAsync(connection, transaction, eventId, unitIds);
                }

                transaction.Commit();
                return eventId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Не удалось создать событие: {ex.Message}", ex);
            }
        }

        // Обновление события и его связей с объектами
        public async Task<bool> UpdateEventWithUnitsAsync(Event updateEvent, IEnumerable<int> unitIds)
        {
            var connectionString = _connectionProvider.GetConnectionString();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. Обновляем событие
                var updated = await UpdateEventAsync(connection, transaction, updateEvent);
                if (!updated)
                {
                    throw new Exception("Событие не найдено для обновления");
                }

                // 2. Удаляем старые связи
                await DeleteEventUnitsAsync(connection, transaction, updateEvent.Id);

                // 3. Добавляем новые связи
                if (unitIds?.Any() == true)
                {
                    await InsertEventUnitsAsync(connection, transaction, updateEvent.Id, unitIds); // Обновление данных по объектам 
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Не удалось обновить событие: {ex.Message}", ex);
            }
        }

        
        

        // Добавляем событие
        private async Task<long> InsertEventAsync(SQLiteConnection connection, SQLiteTransaction transaction, Event newEvent)
        {
            const string query = @"
            INSERT INTO Events (DateEvent, OilRefining, id_category, Description, Action, DateCreate, Creator)
            VALUES (@DateEvent, @OilRefining, @id_category, @Description, @Action, @DateCreate, @Creator);
            SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(query, connection, transaction);
            // Добавление параметров
            command.Parameters.AddWithValue("@DateEvent", newEvent.DateEvent);
            command.Parameters.AddWithValue("@OilRefining", newEvent.OilRefining ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@id_category", newEvent.Id_Category);
            command.Parameters.AddWithValue("@Description", newEvent.Description ?? (object)DBNull.Value); // Если Description == null, вставляем NULL
            command.Parameters.AddWithValue("@Action", newEvent.Action ?? (object)DBNull.Value); // Если Action == null, вставляем NULL
            command.Parameters.AddWithValue("@DateCreate", newEvent.DateCreate);
            command.Parameters.AddWithValue("@Creator", newEvent.Creator);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }


        // Обновление события
        private async Task<bool> UpdateEventAsync(SQLiteConnection connection, SQLiteTransaction transaction, Event updateEvent) // SQL-запрос для обновления данных
        {
            var query = @"" +
                            "UPDATE Events " +
                            "SET DateEvent = @DateEvent, OilRefining = @OilRefining, id_category = @id_category, Description = @Description, Action = @Action " +
                            "WHERE id = @SelectedRowId ";

            using var command = new SQLiteCommand(query, connection, transaction);

            command.Parameters.AddWithValue("@DateEvent", updateEvent.DateEvent);
            command.Parameters.AddWithValue("@OilRefining", updateEvent.OilRefining ?? (object)DBNull.Value); // Если OilRefining == null, вставляем NULL
            command.Parameters.AddWithValue("@id_category", updateEvent.Id_Category);
            command.Parameters.AddWithValue("@Description", updateEvent.Description ?? (object)DBNull.Value); // Если Description == null, вставляем NULL
            command.Parameters.AddWithValue("@Action", updateEvent.Action ?? (object)DBNull.Value); // Если Action == null, вставляем NULL
            command.Parameters.AddWithValue("@SelectedRowId", updateEvent.Id);
            command.ExecuteNonQuery();

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }


        // Добавдяем связь между событием и обьектами
        private async Task InsertEventUnitsAsync(SQLiteConnection connection, SQLiteTransaction transaction, long eventId, IEnumerable<int> unitIds)
        {
            const string insertQuery = "INSERT INTO EventUnits (EventID, UnitID) VALUES (@EventID, @UnitID);";

            foreach (var unitId in unitIds)
            {
                using var command = new SQLiteCommand(insertQuery, connection, transaction);
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@UnitID", unitId);
                await command.ExecuteNonQueryAsync();
            }
        }

        // Удаление записей о установках связанных с собыьием
        private async Task DeleteEventUnitsAsync(SQLiteConnection connection, SQLiteTransaction transaction, long eventId)
        {
            const string query = "DELETE FROM EventUnits WHERE EventID = @EventID";

            using var command = new SQLiteCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@EventID", eventId);
            await command.ExecuteNonQueryAsync();
        }


        /// <summary>
        /// Метод для получения событий из базы данных
        /// </summary>
        /// <param name="queryEvent"></param>
        /// <returns></returns>
        /// 
        public async Task<List<EventForView>> GetEventsAsync(string queryEvent)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            var eventsDict = new Dictionary<int, EventForView>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand(queryEvent, connection);
                using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
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
                                Creator = reader.IsDBNull(reader.GetOrdinal("Creator")) ? null : reader.GetString(reader.GetOrdinal("Creator"))
                            };
                            eventsDict[eventId] = eventModel;
                        }

                        // Если есть прикрепленный файл, добавляем его
                        //if (!reader.IsDBNull(reader.GetOrdinal("FileId")))
                        if (!await reader.IsDBNullAsync(reader.GetOrdinal("FileId")).ConfigureAwait(false))
                        {
                            eventModel.AttachedFiles.Add(new AttachedFileModel() // Передаем строку подключения
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


        public List<EventForView> GetEvents(string queryEvent)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

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
                                Creator = reader.IsDBNull(reader.GetOrdinal("Creator")) ? null : reader.GetString(reader.GetOrdinal("Creator"))
                            };
                            eventsDict[eventId] = eventModel;
                        }

                        // Если есть прикрепленный файл, добавляем его
                        if (!reader.IsDBNull(reader.GetOrdinal("FileId")))
                        {
                            eventModel.AttachedFiles.Add(new AttachedFileModel() // Передаем строку подключения
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



        /// Получить перечень файлов связанных с событием
        public List<AttachedFileForEvent> GetIdFilesOnEvent(int EventId)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            List<AttachedFileForEvent> attachedFile = new List<AttachedFileForEvent>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @" Select FileId, EventId, FileCategory, FileName, FilePath From AttachedFiles Where EventId = @SelectedRowId ";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SelectedRowId", EventId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                attachedFile.Add(new AttachedFileForEvent
                                {
                                    FileId = reader.GetInt32(0),
                                    EventId = reader.GetInt32(1), // ID события с которым связан файл
                                    FileName = reader.IsDBNull(2) ? null : reader.GetString(2), // Имя файла
                                    FilePath = reader.IsDBNull(3) ? null : reader.GetString(3), // Путь к файлу на диске, где он хранится
                                });
                            }
                        }
                    }
                }
                return attachedFile;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Формирует строку SQL запроса для вывода данных в таблицу 
        /// </summary>
        /// <param name="selectedUnit"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public string BuildSQLQueryEvents(Unit selectedUnit, DateTime? startDate, DateTime? endDate, bool isAllEvents)
        {
            var conditions = new List<string>();

            // Фильтрация по установке
            if (selectedUnit != null && selectedUnit.Id != -1)
            {
                string unitName = selectedUnit.UnitName.Replace("'", "''");
                conditions.Add($"Unit LIKE '%{unitName}%'");
            }

            // Фильтрация по времени (если не выбрано "Все даты")
            if (!isAllEvents)
            {
                string startDateStr = startDate?.Date.ToString("yyyy-MM-dd") ?? DateTime.MinValue.Date.ToString("yyyy-MM-dd");
                string endDateStr = endDate?.Date.ToString("yyyy-MM-dd") ?? DateTime.MaxValue.Date.ToString("yyyy-MM-dd");
                conditions.Add($"DateEvent BETWEEN '{startDateStr}' AND '{endDateStr}'");
            }

            // Основной запрос теперь включает LEFT JOIN к AttachedFiles
            string query = @"SELECT e.id, e.DateEvent, e.Unit, e.OilRefining, e.Category, e.Description, e.Action, e.DateCreate, e.Creator, 
                            af.FileId, af.FileCategory, af.FileName, af.FilePath, af.FileSize, af.FileType, af.UploadDate
                            FROM vwEvents e LEFT JOIN AttachedFiles af ON e.id = af.EventId";

            // Добавляем условия, если они есть
            if (conditions.Count > 0)
            {
                query += " WHERE " + string.Join(" AND ", conditions);
            }

            return query;
        }

        /// <summary>
        /// Удалить событие по Id
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteEventAsync(int eventId)
        {
            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (SQLiteTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            await ExecuteDeleteCommandAsync(connection, "DELETE FROM Events WHERE ID = @EventId", eventId);
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку
                Console.WriteLine($"Ошибка при удалении события: {ex.Message}");
                return false;
            }
        }

        private async Task ExecuteDeleteCommandAsync(SQLiteConnection connection, string query, int eventId)
        {
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@EventId", eventId);
                await command.ExecuteNonQueryAsync();
            }
        }

        
    }
}
