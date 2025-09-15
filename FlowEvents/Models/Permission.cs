using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    public class Permission : INotifyPropertyChanged
    {
        private int _permissionId;
        private string _permissionName;
        private string _description;
        private int _IsGranted;

        public int PermissionId
        {
            get => _permissionId;
            set { _permissionId = value; OnPropertyChanged(nameof(PermissionId)); }
        }

        public string PermissionName
        {
            get => _permissionName;
            set { _permissionName = value; OnPropertyChanged(nameof(PermissionName)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }
        
        public int IsGranted
        {
            get => _IsGranted;
            set
            {
                _IsGranted = value;
                OnPropertyChanged(nameof(IsGranted));
                OnPropertyChanged(nameof(IsGrantedBool)); // Уведомляем об изменении bool-свойства
            }
        }

        // Для удобства работы в коде
        public bool IsGrantedBool
        {
            get => _IsGranted != 0;
            set
            {
                IsGranted = value ? 1 : 0;
                // OnPropertyChanged вызывается в сеттере IsGranted
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
