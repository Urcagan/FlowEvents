using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FlowEvents
{
    public class EventsModelForView
    {
        public int Id { get; set; }

        private DateTime _dateEvent;
        public DateTime DateEvent
        {
            get { return _dateEvent; }
            set { _dateEvent = value; }
        }

        private string _dateEventString;
        public string DateEventString
        {
            get { return _dateEventString; }
            set 
            {
                _dateEventString = value;
                // Автоматическое обновление DateTime версии
                if (DateTime.TryParseExact(value, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    DateEvent = date;
                }
            }
        }
        public string OilRefining {  get; set; }   
        public string Unit {  get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public string DateCreate { get; set; }
        public string Creator { get; set; }

        // Новое свойство для хранения списка файлов
        public List<AttachedFileModel> AttachedFiles { get; set; } = new List<AttachedFileModel>();
    }

    public class EventsModel
    {
        public int Id { get; set; }
        public string DateEvent { get; set; }
        public string OilRefining { get; set; }
        public int Id_Category { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public string DateCreate { get; set; }
        public string Creator { get; set; }
    }
}
