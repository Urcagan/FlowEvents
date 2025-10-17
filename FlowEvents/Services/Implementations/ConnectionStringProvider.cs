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
            string _path;
            _path = NormalizeNetworkPath(initialConnectionString);

            //_currentConnectionString = initialConnectionString;
            _currentConnectionString = _path; ;
        }

        public string GetConnectionString()
        {
            return _currentConnectionString;
        }

        public void UpdateConnectionString(string newConnectionString)
        {
            string _path;
            _path = NormalizeNetworkPath(newConnectionString);

            _currentConnectionString = _path; ;
        }


        // Проверка пути к БД , не является ли он сетевым. В случае сетевого пути выполняем необходимое формотирование.
        private string NormalizeNetworkPath(string path)
        {
            string Path;
            //Проверяем не является ли путь сетевым
            if (path.StartsWith("\\"))
            {
                Path = "\\\\" + path;
            }
            else
            {
                Path = path;
            }
            return Path;
        }
    }

}
