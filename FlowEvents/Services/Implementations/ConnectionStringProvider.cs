using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private string _currentConnectionString;

        public ConnectionStringProvider(string initialConnectionString)
        {
            _currentConnectionString = initialConnectionString;
        }

        public string GetConnectionString()
        {
            return _currentConnectionString;
        }

        public void UpdateConnectionString(string newConnectionString)
        {
            _currentConnectionString = newConnectionString;
        }
    }
}
