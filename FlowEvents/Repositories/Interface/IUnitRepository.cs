using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IUnitRepository
    {

        Task<Unit> CreateUnitAsync(Unit unit); //Сохранение новой категории
        Task<Unit> UpdateUnitAsync(Unit unit); // Обновление существующей категории
        Task<bool> DeleteUnitAsync(int unitId); // Удаление категории        
        Task<ObservableCollection<Unit>> GetAllUnitsAsync(); // Получить все объекты
        Task<bool> IsUnitNameUniqueAsync(string name, int? excludeId = null); // // Проверка ктегорию на уникальность


        void UpdateConnectionString(string newConnectionString);
    }
}
