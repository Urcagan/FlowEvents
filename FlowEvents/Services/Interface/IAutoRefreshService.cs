using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Interface
{
    public interface IAutoRefreshService
    {
        bool IsEnabled { get; set; }
        int RefreshInterval { get; set; }
        void Start();
        void Stop();
        event Action RefreshRequested;
    }
}
