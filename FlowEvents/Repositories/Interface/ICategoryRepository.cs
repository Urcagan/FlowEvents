using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface ICategoryRepository
    {

        ObservableCollection<Category> LoadCategories(); // Загрузка категорий из базы данных

        Task<ObservableCollection<Category>> GetAllCategoriesAsync();

        Task<Category> CreateCategoryAsync(Category category); //Сохранение в БД новой категории

        Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null); // // Проверка ктегории на уникальность

        void UpdateConnectionString(string newConnectionString);
    }
}
