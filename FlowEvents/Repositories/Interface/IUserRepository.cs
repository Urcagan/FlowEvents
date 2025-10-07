using FlowEvents.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllUsersAsync(); // Получение всех пользователей
        Task<bool> UserExistsAsync(string username); // Проверка существования пользователя
        Task<int> AddUserAsync(string username, string hashedPassword, string salt, int roleId); // Добавление нового пользователя и возврат его ID
        Task<int> AddDomenUserAsync(string username, string domainName, string displayName, string email, int roleId);
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
