using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class CategoryRepository : ICategoryRepository
    {
        private string _connectionString;
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        //-------------------------
        /// <summary>
        /// Сохранение в БД новой категории
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        //-------------------------
        public async Task<Category> CreateCategoryAsync(Category category)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand(
                    "INSERT INTO Category (Name, Description, Colour) " +
                    "VALUES (@Name, @Description, @Colour); " +
                    "SELECT last_insert_rowid();",
                    connection);

                command.Parameters.AddWithValue("@Name", category.Name);
                command.Parameters.AddWithValue("@Description",
                    string.IsNullOrEmpty(category.Description) ? DBNull.Value : (object)category.Description);
                command.Parameters.AddWithValue("@Colour",
                    string.IsNullOrEmpty(category.Colour) ? DBNull.Value : (object)category.Colour);

                var newId = (long)await command.ExecuteScalarAsync();
                category.Id = (int)newId;

                return category;
            }
        }

        //-------------------------
        /// <summary>
        /// Обновление существующей категории
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        //-------------------------
        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand("UPDATE Category SET Name = @Name, Description = @Description, Colour = @Colour WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Name", category.Name);
                command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(category.Description) ? DBNull.Value : (object)category.Description);
                command.Parameters.AddWithValue("@Colour", string.IsNullOrEmpty(category.Colour) ? DBNull.Value : (object)category.Colour);
                command.Parameters.AddWithValue("@Id", category.Id);
                await command.ExecuteNonQueryAsync();
                return category; // Возвращаем обновленную категорию
            }
        }

        /// <summary>
        /// Удаление категории
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new SQLiteCommand("DELETE FROM Category WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", categoryId);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0; // true если удалено, false если нет
                }
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                // Пробрасываем специальное исключение для ограничений FOREIGN KEY
                throw new InvalidOperationException("Категория используется в записях событий", ex);
            }
        }


        //-------------------------
        /// <summary>
        /// Получить все категории
        /// </summary>
        /// <returns></returns>
        //-------------------------
        public async Task<ObservableCollection<Category>> GetAllCategoriesAsync()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SQLiteCommand("SELECT id, Name, Description, Colour FROM Category", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var categories = new ObservableCollection<Category>();

                    int idIndex = reader.GetOrdinal("id");
                    int nameIndex = reader.GetOrdinal("Name");
                    int descriptionIndex = reader.GetOrdinal("Description");
                    int colourIndex = reader.GetOrdinal("Colour");

                    while (await reader.ReadAsync())
                    {
                        categories.Add(new Category
                        {
                            Id = reader.GetInt32(idIndex),
                            Name = reader.GetString(nameIndex),
                            Description = reader.IsDBNull(descriptionIndex) ? null : reader.GetString(descriptionIndex),
                            Colour = reader.IsDBNull(colourIndex) ? null : reader.GetString(colourIndex)
                        });
                    }
                    return categories;
                }
            }
        }


        //-------------------------
        /// <summary>
        /// Проверка ктегории на уникальность
        /// </summary>
        /// <param name="name"></param>
        /// <param name="excludeId"></param>
        /// <returns></returns>
        //-------------------------
        public async Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM Category WHERE Name = @Name";
                if (excludeId.HasValue)
                {
                    query += " AND Id != @ExcludeId";
                }

                var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Name", name);

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
