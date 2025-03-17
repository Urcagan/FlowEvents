using FlowEvents.Models;
using System.Collections.Generic;

namespace FlowEvents
{
    public interface IDatabaseService
    {
        List<EventsModel> GetEvents();
        void AddEvent(EventsModel newEvent);


        // Другие методы для работы с базой данных
    }
}