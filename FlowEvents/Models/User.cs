using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowEvents.Models
{
    public class User : INotifyPropertyChanged
    {

        private int _id;
        private string _userName;
        private string _domainName;
        private string _displayName;
        private string _email;
        private int _roleId;
        private int _IsAllowed;
        private string _password;
        private string _Salt;


        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        public string DomainName
        {
            get => _domainName;
            set
            {
                _domainName = value;
                OnPropertyChanged(nameof(DomainName));
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public int RoleId
        {
            get => _roleId;
            set
            {
                if (_roleId != value)
                {
                    _roleId = value;
                    OnPropertyChanged(nameof(RoleId));
                    OnPropertyChanged(nameof(RoleName));
                }
            }
        }

        // Свойство для отображения имени роли
        public string RoleName => Role?.RoleName ?? $"RoleId: {RoleId}";

        public Role Role { get; set; }

        public int IsAllowed
        {
            get => _IsAllowed;
            set
            {
                _IsAllowed = value;
                OnPropertyChanged(nameof(IsAllowed));
                OnPropertyChanged(nameof(IsAllowedBool));
            }
        }

        // Свойство-обертка для CheckBox 
        public bool IsAllowedBool
        {
            get => IsAllowed == 1; // Предполагая, что 1 = true, 0 = false
            set
            {
                IsAllowed = value ? 1 : 0;
                OnPropertyChanged(nameof(IsAllowedBool));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

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

        // публичный метод для обновления RoleName
        public void RefreshRoleName()
        {
            OnPropertyChanged(nameof(RoleName));
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
