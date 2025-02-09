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

        private int myVar;

        public int MyProperty
        {
            get { return myVar; }
            set { myVar = value; }
        }


    }

    internal class CategoryModel: INotifyPropertyChanged
    {
        public int id { get; }

        private string _name;
        private string _description;
        private string _colour;

        

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Colour
        {
            get { return _colour; }
            set { _colour = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            //PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }



    }
}
