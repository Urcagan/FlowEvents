using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        //private readonly ILogger<ActiveDirectoryService> _logger;

        //public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger = null)
        //{
        //    _logger = logger;
        //}


        // Метод SearchUsersAsync - Основная логика поиска
        public async Task<DomainSearchResult> SearchUsersAsync(DomainSearchOptions options, CancellationToken cancellationToken = default)
        {
            var result = new DomainSearchResult();

            try
            {
                // Запускаем поиск в отдельном потоке чтобы не блокировать UI
                var users = await Task.Run(() => SearchUsersInternal(options, cancellationToken), cancellationToken);
                
                // Фильтрация только активных пользователей, если включена опция
                if (options.OnlyActive)
                {
                    users = users.Where(u => u.IsActive).ToList();
                }
                
                result.Users = users;
                result.IsSuccess = true;
            }            
            catch (OperationCanceledException)
            {
                // Пользователь отменил операцию - не ошибка, а нормальная ситуация
                //_logger?.LogInformation("Поиск пользователей был отменен");
                result.ErrorMessage = "Операция поиска отменена";
            }
            catch (PrincipalServerDownException ex)
            {
                //_logger?.LogError(ex, "Сервер домена '{DomainController}' недоступен", options.DomainController);
                result.ErrorMessage = $"Сервер домена недоступен: {ex.Message}";
            }
            catch (Exception ex)
            {
                // Любая другая ошибка
                //_logger?.LogError(ex, "Неожиданная ошибка при поиске пользователей");
                result.ErrorMessage = $"Ошибка поиска: {ex.Message}";
            }

            return result;
        }

        // Вспомогательный метод SearchUsersInternal - Непосредственная работа с AD
        private List<DomainUser> SearchUsersInternal(DomainSearchOptions options, CancellationToken cancellationToken)
        {
            var users = new List<DomainUser>();
            int userCount = 0;

            // 1. Подключаемся к домену
            using (var context = CreatePrincipalContext(options.DomainController))
          // using (var context = new PrincipalContext(ContextType.Domain, options.DomainController))
            {
                // 2. Проверяем подключение
                if (!context.ValidateCredentials(null, null, ContextOptions.SimpleBind))
                {
                    throw new PrincipalOperationException($"Не удалось подключиться к контроллеру домена '{options.DomainController}'");
                }

                // 3. Создаем шаблон для поиска
                var userPrincipal = new UserPrincipal(context)
                {
                    //Name = options.SearchTerm // Ищем по имени (можно изменить на EmailAddress и т.д.)
                     Name = $"{options.SearchTerm}*" // Добавляем * для поиска по частичному совпадению
                };

                // 4. Выполняем поиск
                using (var searcher = new PrincipalSearcher(userPrincipal))
                {
                    // 5. Настраиваем таймаут
                    ConfigureSearcherTimeout(searcher, options.Timeout);

                    // 6. Обрабатываем результаты
                    foreach (var principal in searcher.FindAll())
                    {
                        // Проверяем не отменил ли пользователь операцию
                        cancellationToken.ThrowIfCancellationRequested(); // ПРОВЕРКА: если пользователь нажал "Отмена" - прерываем операцию

                        // Ограничиваем количество результатов
                        if (userCount >= options.MaxResults) break;

                        // 7. Преобразуем результат в нашу модель
                        if (principal is UserPrincipal user)
                        {
                            users.Add(new DomainUser
                            {
                                Number = ++userCount,
                                DomainName = context.Name,
                                Username = user.SamAccountName,  // Логин без домена
                                DisplayName = user.DisplayName,  // Полное имя
                                Email = user.EmailAddress,
                                IsActive = user.Enabled ?? true  // Активна ли учетка
                            });
                        }
                    }
                }
            }

            return users;
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password, string domainController = null)
        {
            return await Task.Run(() =>
            {
                using (var context = CreatePrincipalContext(domainController))
                {
                    return context.ValidateCredentials(username, password);
                }
            });
        }

        public async Task<bool> TestDomainConnectionAsync(string domainController)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var context = CreatePrincipalContext(domainController))
                    {
                        return context.ValidateCredentials(null, null, ContextOptions.SimpleBind);
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        private PrincipalContext CreatePrincipalContext(string domainController)
        {
            return new PrincipalContext(ContextType.Domain, domainController);
        }

        private void ConfigureSearcherTimeout(PrincipalSearcher searcher, TimeSpan timeout)
        {
            try
            {
                var directorySearcher = searcher.GetUnderlyingSearcher() as DirectorySearcher;
                if (directorySearcher != null)
                {
                    directorySearcher.ClientTimeout = timeout;
                    directorySearcher.ServerTimeLimit = timeout;
                }
            }
            catch (Exception )
            {
                //_logger?.LogWarning(ex, "Не удалось установить таймаут для поиска");
            }
        }


    }
}
