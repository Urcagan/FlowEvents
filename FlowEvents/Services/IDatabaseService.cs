using FlowEvents.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Services
{
    public interface IDatabaseService
    {
        Task<List<User>> GetUsersAsync();

        void UpdateConnectionString(string newConnectionString);
    }
}
