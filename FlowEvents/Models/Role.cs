using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    //public class Role
    //{
    //    public int RoleId { get; set; }
    //    public string RoleName { get; set; }
    //    public string Description { get; set; }
    //    public List<Permission> Permissions { get; set; } = new List<Permission>();
    //}

    public class Role : INotifyPropertyChanged
    {
        private int _roleId;
        private string _roleName;
        private string _description;

        public int RoleId
        {
            get => _roleId;
            set { _roleId = value; OnPropertyChanged(); }
        }

        public string RoleName
        {
            get => _roleName;
            set { _roleName = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
