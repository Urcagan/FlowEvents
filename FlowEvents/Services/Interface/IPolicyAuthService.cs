using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services
{
    public interface IPolicyAuthService
    {
        bool HasPermission(string dbPath, string username, string permissionName);
        bool HasAnyPermission(string dbPath, string username, params string[] permissionNames); //Комбинированная проверка
                                                                                                
        List<string> GetUserPermissions(string username);// метод: загружает ВСЕ права пользователя

        User GetUser(string username);
        void ReloadPolicies();  // Для обновления кэша
    }
}
