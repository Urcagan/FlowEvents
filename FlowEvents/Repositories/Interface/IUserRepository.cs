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
        Task<List<User>> GetAllUsersAsync(); // Получение всех пользователей
        Task<bool> UserExistsAsync(string username); // Проверка существования пользователя
        Task<int> AddUserAsync(string username, string hashedPassword, string salt, int roleId); // Добавление нового пользователя и возврат его ID
        Task<bool> DeleteUserAsync(string userName);

        Task UpdateUserAccessAsync(string username, bool isAllowed);
        Task UpdateUserRoleAsync(string username, int newRoleId);

        //--------------- Интерфейсы которые понадобятся ------------------------

        //     Task<User> GetUserByIdAsync(int id);
        //     Task AddUserAsync(User user);
        //     Task UpdateUserAsync(User user);
        //     Task DeleteUserAsync(int id);

    }
}
