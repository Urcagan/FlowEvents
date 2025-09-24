using FlowEvents.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Services
{
    public interface IDatabaseService
    {
        Task<List<User>> GetUsersAsync();
        Task<List<Role>> GetRolesAsync();
        Task<List<Permission>> GetPermissionsAsync();


        //Task<List<Permission>> GetRolePermissionsAsync(int roleId);

        //       Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
        //       Task UpdateUserRoleAsync(int userId, int roleId);

    }
}
