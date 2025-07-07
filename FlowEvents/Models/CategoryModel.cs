using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    public class CategoryModel : INotifyPropertyChanged
    {
        private int _id;
        public int Id
        {
            get => _id; 
            set { _id = value; }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value; 
                OnPropertyChanged(nameof(Name));
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set 
            { 
                _description = value; 
                OnPropertyChanged(nameof(Description));
            }
        }

        private string _colour;
        public string Colour
        {
            get { return _colour; }
            set 
            {
                _colour = value; 
                OnPropertyChanged(nameof(Colour));
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
