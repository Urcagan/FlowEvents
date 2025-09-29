using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Interface
{
    public interface IUserService
    {
        Task<(bool Success, string Message)> RegisterUserAsync(string username, string password, string confirmPassword, int DefaultRoleID );

        Task<(bool Success, string Message)> RegisterDomainUserAsync(string username, string domainName, string displayName, string email, int DefaultRoleID);

        Task<List<User>> GetUsersAsync();

        Task UpdateUserAccessAsync(string username, bool isAllowed);

        Task<bool> DeleteUserAsync(string userName);

        Task ChangeUserRoleAsync(string userName, int newRoleId);
    }
}
