using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowEvents
{
    // Модель пользователя домена
    public class DomainUser : INotifyPropertyChanged
    {
        private int _number;            // Порядковый номер в результатах
        private string _domainName;     // Имя домена (например, "COMPANY")
        private string _username;       // Логин (SamAccountName - "john.doe")
        private string _displayName;    // Отображаемое имя ("John Doe")
        private string _email;          // Email адрес
        private bool _isActive;         // Активна ли учетная запись

        public int Number
        {
            get { return _number; }
            set { _number = value; OnPropertyChanged(); }
        }

        public string DomainName
        {
            get => _domainName;
            set { _domainName = value; OnPropertyChanged(); }
        }

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public bool IsActive 
        { 
            get => _isActive; 
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    // Параметры поиска
    public class DomainSearchOptions
    {
        public string SearchTerm { get; set; } = "*";         // Что ищем ("john", "doe", "*" - все)
        public string DomainController { get; set; }          // Контроллер домена ("dc1.company.com")
        public int MaxResults { get; set; } = 10;             // Максимум результатов (ограничение)
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10); // Таймаут операции
    }
}
