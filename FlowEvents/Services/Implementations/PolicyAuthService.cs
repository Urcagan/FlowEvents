using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services
{
    public class PolicyAuthService : IPolicyAuthService
    {

        // Кэш политик (если нужен)
        private Dictionary<string, Dictionary<string, HashSet<string>>> _policyCache = new Dictionary<string, Dictionary<string, HashSet<string>>>();

      
        public bool HasPermission(string dbPath, string username, string permissionName)
        {
            var user = GetUser(dbPath, username);
            if (user == null || user.IsAllowed == 0)
                return false;

            // Загружаем политики для БД, если их нет в кэше
            if (!_policyCache.ContainsKey(dbPath))
                ReloadPolicies(dbPath);

            if (_policyCache.TryGetValue(dbPath, out var dbPolicies) &&
                dbPolicies.TryGetValue(permissionName, out var allowedRoles))
            {
                return allowedRoles.Contains(user.Role.RoleName);
            }

            return false;
        }

        public bool HasAnyPermission(string dbPath, string username, params string[] permissionNames)
        {
            if (string.IsNullOrEmpty(dbPath) || string.IsNullOrEmpty(username))
                return false;

            foreach (var permission in permissionNames)
            {
                if (HasPermission(dbPath, username, permission))
                    return true;
            }
            return false;
        }

        public List<string> GetUserPermissions(string dbPath, string username)
        {
            var permissions = new List<string>();

            using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                                    SELECT p.PermissionName
                                    FROM Users u
                                    JOIN RolePermissions rp ON u.RoleId = rp.RoleId
                                    JOIN Permissions p ON rp.PermissionId = p.PermissionId
                                    WHERE u.UserName = @username";
                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        permissions.Add(reader.GetString(0));
                    }
                }
            }

            return permissions;
        }

        public User GetUser(string dbPath, string username)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;foreign keys=true;"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT u.UserName, u.IsAllowed, r.RoleId, r.RoleName
                FROM Users u
                JOIN Roles r ON u.RoleId = r.RoleId
                WHERE u.UserName = @username";
                command.Parameters.AddWithValue("@username", username);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            UserName = reader.GetString(0),
                            IsAllowed = reader.GetInt32(1),
                            Role = new Role
                            {
                                RoleId = reader.GetInt32(2),
                                RoleName = reader.GetString(3)
                            }
                        };
                    }
                }
            }
            return null;



        }

        public void ReloadPolicies(string dbPath)
        {
            var policies = new Dictionary<string, HashSet<string>>();

            using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT p.PermissionName, r.RoleName
                FROM RolePermissions rp
                JOIN Permissions p ON rp.PermissionId = p.PermissionId
                JOIN Roles r ON rp.RoleId = r.RoleId";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var permission = reader.GetString(0);
                        var role = reader.GetString(1);

                        if (!policies.ContainsKey(permission))
                            policies[permission] = new HashSet<string>();

                        policies[permission].Add(role);
                    }
                }
            }

            _policyCache[dbPath] = policies;
        }
    }
}
