using System;
using System.Collections.Generic;

namespace FlowEvents.Models
{

    // Легковесная модель разрешения для кеша (без INotifyPropertyChanged)
    public class CachedPermission
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; }
        public string Description { get; set; }
    }

    // Легковесная модель роли для кеша с правами
    public class CachedRole
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public HashSet<int> PermissionIds { get; set; } = new HashSet<int>(); // Быстрый поиск прав
    }

    // Главный контейнер кеша
    public class RolePermissionsCache
    {
        public Dictionary<int, CachedRole> Roles { get; set; } = new Dictionary<int, CachedRole>();
        public Dictionary<int, CachedPermission> Permissions { get; set; } = new Dictionary<int, CachedPermission>();
        public Dictionary<string, int> PermissionNameToId { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public DateTime LastUpdated { get; set; }
        public bool IsLoaded => Roles.Count > 0 && Permissions.Count > 0;
    }
}
