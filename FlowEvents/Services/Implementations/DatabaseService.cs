using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IEventRepository _eventRepository;

        // Внедрение зависимости через конструктор
        public DatabaseService(IUserRepository userRepository,
                            IRoleRepository roleRepository,
                            IPermissionRepository permissionRepository,
                            IEventRepository eventRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _eventRepository = eventRepository;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }


        public async Task<List<Role>> GetRolesAsync()
        {
            return await _roleRepository.GetAllRolesAsync();
        }


        public async Task<List<Permission>> GetPermissionsAsync()
        {
            return await _permissionRepository.GetAllPermissionsAsync();
        }


        public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
        {
            return await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
        }


        //public async Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        //{
        //    await _permissionRepository.UpdateRolePermissionsAsync(roleId, permissionIds);
        //}

        //public async Task UpdateUserRoleAsync(int userId, int roleId)
        //{
        //    var user = await _userRepository.GetUserByIdAsync(userId);
        //    if (user != null)
        //    {
        //        user.RoleId = roleId;
        //        await _userRepository.UpdateUserAsync(user);
        //    }
        //}


        //-----------------------------------------------------------------------
        public void UpdateConnectionString(string newConnectionString)
        {
            _userRepository.UpdateConnectionString(newConnectionString);
            _roleRepository.UpdateConnectionString(newConnectionString);
            _permissionRepository.UpdateConnectionString(newConnectionString);
            _eventRepository.UpdateConnectionString(newConnectionString);

            // Обновляем глобальную переменную
            Global_Var.ConnectionString = newConnectionString;
        }
    }
}
