using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<List<Permission>> GetPermissionsAsync()
        {
            return await _permissionRepository.GetAllPermissionsAsync();
        }

        public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
        {
            return await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
        }

    }
}
