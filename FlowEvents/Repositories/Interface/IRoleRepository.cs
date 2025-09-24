using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IRoleRepository
    {

        Task<List<Role>> GetAllRolesAsync();

        Task<bool> RoleExistsAsync(int roleID);


        //      Task<Role> GetRoleByIdAsync(int id);
        //      Task AddRoleAsync(Role role);
        //      Task UpdateRoleAsync(Role role);
        //      Task DeleteRoleAsync(int id);

    }
}
