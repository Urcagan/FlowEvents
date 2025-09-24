using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowEvents
{
    public class DomainUser : INotifyPropertyChanged
    {
        private int _number;
        public int Number
        {
            get { return _number; }
            set { _number = value; OnPropertyChanged(); }
        }


        private string _username;
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _domainName;
        public string DomainName
        {
            get => _domainName;
            set { _domainName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
