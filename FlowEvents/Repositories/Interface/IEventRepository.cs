using FlowEvents.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FlowEvents.Repositories.Interface
{
    public interface IEventRepository
    {
        List<EventForView> GetEvents(string queryEvent);
        ObservableCollection<Unit> GetUnitFromDatabase();  //Загрузка перечня установок из ДБ

        void UpdateConnectionString(string newConnectionString);
    }
}
