using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Interface
{
    public interface IDatabaseInfoRepository
    {
        Task<string> GetDatabaseVersionAsync(string connectionString);
        Task<bool> CheckDatabaseStructureAsync(string connectionString);
        Task<bool> TestConnectionAsync(string connectionString);
    }
}
