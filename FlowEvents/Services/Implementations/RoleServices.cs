using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class RoleServices : IRoleServices
    {

        private readonly IRoleRepository _roleRepository;
        
        
        public RoleServices(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }
        
        public async Task<List<Role>> GetRolesAsync()
        {
            return await _roleRepository.GetAllRolesAsync();
        }


    }
}
