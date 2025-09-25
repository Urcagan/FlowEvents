using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowEvents.Services.Interface
{
    public interface IActiveDirectoryService
    {
        //Task<List<DomainUser>> SearchUsersAsync(DomainSearchOptions options, CancellationToken cancellationToken = default);

        // Основной метод поиска пользователей
        Task<DomainSearchResult> SearchUsersAsync(DomainSearchOptions options, CancellationToken cancellationToken = default);

        // Проверка логина/пароля (аутентификация)
        Task<bool> ValidateCredentialsAsync(string username, string password, string domainController = null);

        // Проверка доступности домена
        Task<bool> TestDomainConnectionAsync(string domainController);
    }

    // DomainSearchResult: Обертка для возврата результата + метаданные об операции
    public class DomainSearchResult
    {
        public List<DomainUser> Users { get; set; } = new List<DomainUser>();
        public bool IsSuccess { get; set; }       // Успешно ли выполнен поиск
        public string ErrorMessage { get; set; }  // Сообщение об ошибке (если IsSuccess = false)
    }
}
