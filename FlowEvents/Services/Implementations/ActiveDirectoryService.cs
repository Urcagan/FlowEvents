using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {

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
                result.ErrorMessage = "Операция поиска отменена";
            }
            catch (PrincipalServerDownException ex)
            {
                result.ErrorMessage = $"Сервер домена недоступен: {ex.Message}";
            }
            catch (Exception ex)
            {
                // Любая другая ошибка
                result.ErrorMessage = $"Ошибка поиска: {ex.Message}";
            }

            return result;
        }

        // Вспомогательный метод SearchUsersInternal - Непосредственная работа с AD
        private List<DomainUser> SearchUsersInternal(DomainSearchOptions options, CancellationToken cancellationToken)
        {
            var users = new List<DomainUser>();
            int userCount = 0;

            using (var context = new PrincipalContext(ContextType.Domain, options.DomainController))
            {
                if (!context.ValidateCredentials(null, null, ContextOptions.SimpleBind))
                {
                    throw new PrincipalOperationException($"Не удалось подключиться к домену");
                }

                var userPrincipal = new UserPrincipal(context)
                {
                    Name = $"{options.SearchTerm}*"
                };

                using (var searcher = new PrincipalSearcher(userPrincipal))
                {
                    // 🔧 НАСТРАИВАЕМ PAGING
                    var directorySearcher = searcher.GetUnderlyingSearcher() as DirectorySearcher;
                    if (directorySearcher != null)
                    {
                        directorySearcher.PageSize = options.MaxResults;
                        directorySearcher.ServerTimeLimit = TimeSpan.FromSeconds(30);
                    }

                    try
                    {
                        foreach (var principal in searcher.FindAll())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (userCount >= options.MaxResults) break;

                            try
                            {
                                if (principal is UserPrincipal user)
                                {
                                    bool isActive = user.Enabled ?? true;

                                    // ФИЛЬТРАЦИЯ ПО АКТИВНОСТИ
                                    if (options.OnlyActive && !isActive) continue;

                                    // 🔒 БЕЗОПАСНОЕ ПОЛУЧЕНИЕ ДАННЫХ
                                    users.Add(new DomainUser
                                    {
                                        Number = ++userCount,
                                        DomainName = context.Name ?? options.DomainController,
                                        Username = user.SamAccountName ?? "Не указан",
                                        DisplayName = user.DisplayName ?? "Не указано",
                                        Email = user.EmailAddress ?? "Не указан",
                                        IsActive = isActive
                                    });
                                }
                            }
                            catch (Exception )
                            {
                                // 🚨 ЛОГИРУЕМ ОШИБКУ ОДНОГО ПОЛЬЗОВАТЕЛЯ
                                //System.Diagnostics.Debug.WriteLine($"Ошибка обработки пользователя: {ex.Message}");
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 🚨 ЛОГИРУЕМ ОБЩУЮ ОШИБКУ ПОИСКА
                        throw new Exception($"Ошибка при поиске пользователей: {ex.Message}", ex);
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
