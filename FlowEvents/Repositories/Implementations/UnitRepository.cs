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
                    "VALUES (@Unit, @Description)",
                    connection);

                command.Parameters.AddWithValue("@Unit", unit.UnitName);
                command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(unit.Description) ? DBNull.Value : (object)unit.Description);
                command.ExecuteNonQuery();

                var newId = (long)await command.ExecuteScalarAsync(); // Получаем ID новой записи
                unit.Id = (int)newId; // Присваиваем ID новой записи объекту newUnit

                return unit;
            }
        }

        public async Task<Unit> UpdateUnitAsync(Unit unit)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteUnitAsync(int unitId)
        {
            throw new NotImplementedException();
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

        public async Task<bool> IsUnitNameUniqueAsync(string name, int? excludeId = null)
        {
            throw new NotImplementedException();
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
