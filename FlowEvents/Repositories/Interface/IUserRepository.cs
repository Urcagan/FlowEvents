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
        void UpdateConnectionString(string newConnectionString);
    }
}
