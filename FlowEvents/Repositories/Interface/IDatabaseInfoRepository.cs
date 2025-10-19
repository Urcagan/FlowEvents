using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IDatabaseInfoRepository
    {
        Task<int> GetUserVersion(string connectionString);
        Task<bool> CheckDatabaseStructureAsync(string connectionString);
        Task<bool> TestConnectionAsync(string connectionString);
    }
}
