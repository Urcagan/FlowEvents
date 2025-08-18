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
        User GetUser(string dbPath, string username);
        void ReloadPolicies(string dbPath);  // Для обновления кэша
    }
}
