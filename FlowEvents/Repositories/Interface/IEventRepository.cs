using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IEventRepository
    {
        List<EventForView> GetEvents(string queryEvent);
        ObservableCollection<Unit> GetUnitFromDatabase();  //Загрузка перечня установок из ДБ
        List<AttachedFileForEvent> GetIdFilesOnEvent(int EventId); //Получение списка файлов, прикрепленных к событию
        string BuildSQLQueryEvents(Unit selectedUnit, DateTime? startDate, DateTime? endDate, bool isAllEvents); // Формирует строку SQL запроса для вывода данных в таблицу
        Task<bool> DeleteEventAsync(int eventId); // Удалить событие по Id

        void UpdateConnectionString(string newConnectionString);
    }
}
