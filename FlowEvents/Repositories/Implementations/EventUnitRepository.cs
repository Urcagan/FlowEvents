using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class EventUnitRepository : IEventUnitRepository
    {        
        private readonly IConnectionStringProvider _connectionProvider;                
        public EventUnitRepository(IConnectionStringProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }


        /// <summary>
        /// Возвращает список UnitID для данного EventID 
        /// </summary>
        /// <param name="connectionProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<List<int>> GetIdUnitForEventAsync(long eventId)
        {
            // Валидация входных параметров
            if (eventId <= 0)
                throw new ArgumentException("Event ID Должен быть больше 0", nameof(eventId));

            var _connectionString = _connectionProvider.GetConnectionString(); // Получение актуальной строки подключения
            var unitIds = new List<int>();            

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string query = "SELECT UnitID FROM EventUnits WHERE EventID = @eventId";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@eventId", eventId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        int unitIdIndex = reader.GetOrdinal("UnitID"); //Получаем индекс "UnitID"

                        while (await reader.ReadAsync())
                        {
                            unitIds.Add(reader.GetInt32(unitIdIndex));
                        }
                    }
                }
            }
            return unitIds; // Возвращает пустой список если связей не найдено
        }


        public async Task AddEventUnitsAsync(long eventId, IEnumerable<int> unitIds) // Добавдяем связь между событием и обьектами
        {
            var connectionString = _connectionProvider.GetConnectionString();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            const string insertQuery = "INSERT INTO EventUnits (EventID, UnitID) VALUES (@EventID, @UnitID);";

            foreach (var unitId in unitIds)
            {
                using var command = new SQLiteCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@UnitID", unitId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
