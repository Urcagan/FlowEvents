using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllUsersAsync();

        Task AddLocalUserToDatabaseAsync(string username, string hashedPassword, string salt, int RoleId);


        void UpdateConnectionString(string newConnectionString);

        //--------------- Интерфейсы которые понадобятся ------------------------

        //     Task<User> GetUserByIdAsync(int id);
        //     Task AddUserAsync(User user);
        //     Task UpdateUserAsync(User user);
        //     Task DeleteUserAsync(int id);

    }
}
