using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FlowEvents
{
    // Модель пользователя домена
    public class DomainUser : INotifyPropertyChanged
    {
        private int _number;            // Порядковый номер в результатах
        private string _domainName;     // Имя домена (например, "COMPANY")
        private string _username;       // Логин (SamAccountName - "john.doe")
        private string _displayName;    // Отображаемое имя ("John Doe")
        private string _title;          // Должность
        private string _department;    // Подразделение
        private string _company;        // Компания
        private string _email;          // Email адрес
        private string _telephoneNumber;
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
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }
        public string Department
        {
            get => _department;
            set { _department = value; OnPropertyChanged(); }
        }
        public string Company
        {
            get => _company;
            set { _company = value; OnPropertyChanged(); }
        }
        public string TelephoneNumber
        {
            get => _telephoneNumber;
            set { _telephoneNumber = value; OnPropertyChanged(); }
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
        public int MaxResults { get; set; } = 50;             // Максимум результатов (ограничение)
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10); // Таймаут операции
        public bool OnlyActive { get; set; } = false;
    }

}
