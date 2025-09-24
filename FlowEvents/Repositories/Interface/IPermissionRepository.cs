using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<List<Permission>> GetPermissionsByRoleIdAsync(int roleId);

        //     Task GrantPermissionAsync(int roleId, int permissionId);
        //     Task RevokePermissionAsync(int roleId, int permissionId);
        //     Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);

    }
}
