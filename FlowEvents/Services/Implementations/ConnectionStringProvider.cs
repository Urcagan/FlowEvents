using FlowEvents.Services.Interface;

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


        public string CreateConnectionString(string databasePath)
        {

            string Path;
            //Проверяем не является ли путь сетевым
            if (databasePath.StartsWith("\\"))
            {
                Path = "\\\\" + databasePath;
            }
            else
            {
                Path = databasePath;
            }

            return $"Data Source={Path};Version=3;foreign keys=true;";
        }


    }

}
