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


        //-------------------------------------------------------------------
        // Метод для обновления строки подключения во время работы приложения
        public void UpdateConnectionString(string newConnectionString)
        {
            _connectionString = newConnectionString;
        }
    }
}
