using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    public class EventsModel
    {
        public int Id { get; set; }

        private string _dateEvent;

        public string DateEvent
        {
            get { return _dateEvent; }
            set { _dateEvent = value; }
        }

        public int UnitID {  get; set; }
        public int Category { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
    }

    
}
