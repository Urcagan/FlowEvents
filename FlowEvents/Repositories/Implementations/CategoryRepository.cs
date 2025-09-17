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
    public class CategoryRepository : ICategoryRepository
    {
        private string _connectionString;
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }



        // Загрузка категорий из базы данных
        public ObservableCollection<Category> LoadCategories()
        {
            var Categories = new ObservableCollection<Category>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("SELECT id, Name, Description, Colour FROM Category", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        int idIndex = reader.GetOrdinal("id");
                        int nameIndex = reader.GetOrdinal("Name");
                        int descriptionIndex = reader.GetOrdinal("Description");
                        int colourIndex = reader.GetOrdinal("Colour");

                        while (reader.Read())
                        {
                            var category = new Category
                            {
                                Id = reader.GetInt32(idIndex),
                                Name = reader.GetString(nameIndex),
                                Description = reader.IsDBNull(descriptionIndex) ? null : reader.GetString(descriptionIndex),
                                Colour = reader.IsDBNull(colourIndex) ? null : reader.GetString(colourIndex)
                            };
                            Categories.Add(category);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return Categories;
        }


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

        //Сохранение в БД новой категории
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


        // Проверка ктегории на уникальность
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
        // Метод для обновления строки подключения во время работы приложения
        public void UpdateConnectionString(string newConnectionString)
        {
            _connectionString = newConnectionString;
        }
    }
}
