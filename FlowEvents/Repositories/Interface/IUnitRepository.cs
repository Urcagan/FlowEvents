using FlowEvents.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IUnitRepository
    {
        Task<Unit> CreateUnitAsync(Unit unit); //Сохранение новой категории
        Task<Unit> UpdateUnitAsync(Unit unit); // Обновление существующей категории
        Task<bool> DeleteUnitAsync(int unitId); // Удаление категории        
        Task<bool> IsUnitNameUniqueAsync(string unit, int? excludeId = null); // // Проверка ктегорию на уникальность
        Task<ObservableCollection<Unit>> GetAllUnitsAsync(); // Получить все объекты

    }
}
