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

        public string Unit {  get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public string DateCreate { get; set; }
        public string Creator { get; set; }
    }

    
}
