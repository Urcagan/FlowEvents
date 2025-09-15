using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowEvents.Models
{
    public class User : INotifyPropertyChanged
    {

        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        private string _domainName;
        public string DomainName
        {
            get => _domainName;
            set
            {
                _domainName = value;
                OnPropertyChanged(nameof(DomainName));
            }
        }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        private int _roleId;
        public int RoleId
        {
            get => _roleId;
            set
            {
                _roleId = value;
                OnPropertyChanged(nameof(RoleId));
            }
        }

        public Role Role { get; set; }

        private int _IsAllowed;
        public int IsAllowed
        {
            get => _IsAllowed;
            set
            {
                _IsAllowed = value;
                OnPropertyChanged(nameof(IsAllowed));
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
        private string _Salt;
        public string Salt
        {
            get => _Salt;
            set
            {
                _Salt = value;
                OnPropertyChanged(nameof(Salt));
            }
        }
        public bool IsAuthenticated { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
