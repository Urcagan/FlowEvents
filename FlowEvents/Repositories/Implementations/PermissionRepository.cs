using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    internal class PermissionRepository : IPermissionRepository
    {
        public Task<List<Permission>> GetAllPermissionsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<Permission>> GetPermissionsByRoleIdAsync(int roleId)
        {
            throw new NotImplementedException();
        }
    }
}
