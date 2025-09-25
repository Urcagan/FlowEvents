using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.ViewModels
{
    public class UserInfoViewModel : INotifyPropertyChanged
    {
        private string _login = string.Empty;
        private string _displayName = string.Empty;
        private string _fullLogin = string.Empty;
        private string _domain = string.Empty;
        private string _email = string.Empty;
        private string _distinguishedName = string.Empty;
        private bool _isDomainUser;
        private string _sid = string.Empty;
        private string _description = string.Empty;

        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(nameof(Login)); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        public string FullLogin
        {
            get => _fullLogin;
            set { _fullLogin = value; OnPropertyChanged(nameof(FullLogin)); }
        }

        public string Domain
        {
            get => _domain;
            set { _domain = value; OnPropertyChanged(nameof(Domain)); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        public string DistinguishedName
        {
            get => _distinguishedName;
            set { _distinguishedName = value; OnPropertyChanged(nameof(DistinguishedName)); }
        }

        public bool IsDomainUser
        {
            get => _isDomainUser;
            set { _isDomainUser = value; OnPropertyChanged(nameof(IsDomainUser)); OnPropertyChanged(nameof(UserType)); }
        }

        public string SID
        {
            get => _sid;
            set { _sid = value; OnPropertyChanged(nameof(SID)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string UserType => IsDomainUser ? "Доменный пользователь" : "Локальный пользователь";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"Логин: {Login}\n" +
                   $"Отображаемое имя: {DisplayName}\n" +
                   $"Полный логин: {FullLogin}\n" +
                   $"Домен: {Domain}\n" +
                   $"Email: {Email}\n" +
                   $"Тип: {UserType}\n" +
                   $"SID: {SID}\n" +
                   $"Описание: {Description}\n" +
                   $"Distinguished Name: {DistinguishedName}";
        }
    }
}
