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

        void UpdateConnectionString(string newConnectionString);
    }
}
