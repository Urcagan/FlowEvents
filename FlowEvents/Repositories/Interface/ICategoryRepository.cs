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
        Task<Category> CreateCategoryAsync(Category category); //Сохранение новой категории
        Task<Category> UpdateCategoryAsync(Category category); // Обновление существующей категории
        Task<bool> DeleteCategoryAsync(int categoryId); // Удаление категории
        Task<bool> IsCategoryNameUniqueAsync(string name, int? excludeId = null); // // Проверка ктегорию на уникальность
        Task<ObservableCollection<Category>> GetAllCategoriesAsync(); // Получить все категории 
           

        void UpdateConnectionString(string newConnectionString);


    }
}
