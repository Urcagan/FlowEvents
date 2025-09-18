using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents.Repositories
{
    public class EventRepository : IEventRepository
    {
        private string _connectionString;
        public EventRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }


        /// <summary>
        /// Метод для получения событий из базы данных
        /// </summary>
        /// <param name="queryEvent"></param>
        /// <returns></returns>
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
                                Creator = reader.IsDBNull(reader.GetOrdinal("Creator")) ? null : reader.GetString(reader.GetOrdinal("Creator"))
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

        /// <summary>
        /// Загрузка перечня установок из ДБ
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Unit> GetUnitFromDatabase()
        {
            var units = new ObservableCollection<Unit>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString)) //_connectionString
                {
                    connection.Open();

                    string query = @" Select id, Unit, Description From Units ";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            units.Add(new Unit
                            {
                                Id = reader.GetInt32(0),
                                UnitName = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
            return units;
        }



        /// <summary>
        /// Получить перечень файлов связанных с событием
        /// </summary>
        /// <param name="EventId"></param>
        /// <returns> List<AttachedFileForEvent> </returns>
        public List<AttachedFileForEvent> GetIdFilesOnEvent(int EventId)
        {
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



        //-------------------------------------------------------------------
        /// <summary>
        /// Метод для обновления строки подключения во время работы приложения
        /// </summary>
        /// <param name="newConnectionString"> Строка с нового подключения </param>
        //-------------------------------------------------------------------
        public void UpdateConnectionString(string newConnectionString)
        {
            _connectionString = newConnectionString;
        }
    }
}
