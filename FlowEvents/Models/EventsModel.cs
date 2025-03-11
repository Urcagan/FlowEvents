using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    internal class EventsModel
    {
        public int Id { get; set; }

        private DateTime _EventDateTime;

        public DateTime EventDateTime
        {
            get { return _EventDateTime; }
            set { _EventDateTime = value; }
        }

        public string Unit { get; set; }
    }

    
}
