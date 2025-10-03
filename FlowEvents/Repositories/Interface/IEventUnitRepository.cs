using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    // Работа с таблицей EvetnUnits
    public interface IEventUnitRepository
    {
        //Task UpdateEventUnitsAsync(long eventId, List<int> unitIds);
        Task<List<int>> GetIdUnitForEventAsync(int eventId); // Возвращает список UnitID для данного EventID
        Task AddEventUnitsAsync(long eventId, IEnumerable<int> unitIds); // Добавдяем связь между событием и обьектами
        //Task DeleteEventUnitsAsync(long eventId);
    }
}
