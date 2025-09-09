using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowEvents.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IUserRepository _userRepository;

        // Внедрение зависимости через конструктор
        public DatabaseService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public void UpdateConnectionString(string newConnectionString)
        {
            _userRepository.UpdateConnectionString(newConnectionString);

            // Обновляем глобальную переменную
            Global_Var.ConnectionString = newConnectionString;
        }
    }
}
