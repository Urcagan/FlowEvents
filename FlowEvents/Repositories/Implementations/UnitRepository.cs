using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class UnitRepository : IUnitRepository
    {
        private string _connectionString;
        public UnitRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<Unit> CreateUnitAsync(Unit unit)
        {

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand(
                    "INSERT INTO Units (Unit, Description) " +
                    "VALUES (@Unit, @Description); " +
                    "SELECT last_insert_rowid();",
                    connection);

                command.Parameters.AddWithValue("@Unit", unit.UnitName);
                command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(unit.Description) ? DBNull.Value : (object)unit.Description);

                var newId = (long)await command.ExecuteScalarAsync(); // Получаем ID новой записи
                unit.Id = (int)newId; // Присваиваем ID новой записи объекту newUnit

                return unit;
            }
        }

        //----------------------------------
        //    Обновление в базе данных 
        //----------------------------------
        public async Task<Unit> UpdateUnitAsync(Unit unit)
        {            
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand(
                    "UPDATE Units SET Unit = @Unit, Description = @Description WHERE Id = @Id",
                    connection);
                command.Parameters.AddWithValue("@Unit", unit.UnitName);
                command.Parameters.AddWithValue("@Description", unit.Description);
                command.Parameters.AddWithValue("@Id", unit.Id);
                await command.ExecuteNonQueryAsync();
                return unit;
            }
        }

        //Удаление
        public async Task<bool> DeleteUnitAsync(int unitId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var command = new SQLiteCommand("DELETE FROM Units WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", unitId);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0; // true если удалено, false если нет
                }
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                // Пробрасываем специальное исключение для ограничений FOREIGN KEY
                throw new InvalidOperationException("Объект используется в записях событий", ex);
            }
            
        }



        //----------------------------------
        //      Получить все объекты       |
        //----------------------------------
        public async Task<ObservableCollection<Unit>> GetAllUnitsAsync() // Получить все объекты
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand("SELECT * FROM Units", connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var units = new ObservableCollection<Unit>();

                    int idIndex = reader.GetOrdinal("id");
                    int unitIndex = reader.GetOrdinal("Unit");
                    int descriptionIndex = reader.GetOrdinal("Description");

                    while (await reader.ReadAsync())
                    {
                        units.Add(new Unit
                        {
                            Id = reader.GetInt32(idIndex),
                            UnitName = reader.GetString(unitIndex),
                            Description = reader.IsDBNull(descriptionIndex) ? null : reader.GetString(descriptionIndex)
                        });
                    }
                    return units;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="excludeId"></param>
        /// <returns></returns>
        public async Task<bool> IsUnitNameUniqueAsync(string unit, int? excludeId = null)
        {

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM Units WHERE Unit = @Unit";
                if (excludeId.HasValue)
                {
                    query += " AND Id != @ExcludeId";
                }

                var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Unit", unit);

                if (excludeId.HasValue)
                {
                    command.Parameters.AddWithValue("@ExcludeId", excludeId.Value);
                }

                var count = (long)await command.ExecuteScalarAsync();
                return count == 0;
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
