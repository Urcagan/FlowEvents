using FlowEvents.Models;
using System.Collections.Generic;

namespace FlowEvents.Repositories.Interface
{
    public interface IEventRepository
    {
        List<EventsModelForView> GetEvents(string queryEvent);
    }
}
