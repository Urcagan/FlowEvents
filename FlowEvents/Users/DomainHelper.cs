using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class DomainHelper
    {
        /**
        public static List<DomainUserModel> GetDomainUsers()
        {
            var users = new List<DomainUserModel>();

            using (var context = new PrincipalContext(ContextType.Domain))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    foreach (var result in searcher.FindAll())
                    {
                        if (result is UserPrincipal user)
                        {
                            users.Add(new DomainUserModel
                            {
                                DomainName = context.Name,
                                Username = user.SamAccountName,
                                DisplayName = user.DisplayName,
                                Email = user.EmailAddress
                            });
                        }
                    }
                }
            }
            return users;
        }
        **/

        /**
        public static List<DomainUserModel> FindDomainUser(string userToFind)
        {
            var users = new List<DomainUserModel>();

            using (var context = new PrincipalContext(ContextType.Domain))
            {
                // Создаем шаблон пользователя для поиска
                var userPrincipal = new UserPrincipal(context)
                {
                    SamAccountName = userToFind, // Ищем по SamAccountName (логин без домена)
                    // ИЛИ можно искать по имени (DisplayName) Name = userToFind,
                    // ИЛИ по email EmailAddress = userToFind
                };

                using (var searcher = new PrincipalSearcher(userPrincipal))
                {
                    int maxResults = 5; //Ограничение количества результатов
                    foreach (var result in searcher.FindAll())
                    {
                        if (users.Count >= maxResults) break;
                        if (result is UserPrincipal user)
                        {
                            users.Add(new DomainUserModel
                            {
                                DomainName = context.Name,
                                Username = user.SamAccountName,
                                DisplayName = user.DisplayName,
                                Email = user.EmailAddress
                            });
                            MessageBox.Show("DomainName = " + context.Name + ", Username = " + user.SamAccountName + ", DisplayName = " + user.DisplayName + ", Email = " + user.EmailAddress);
                        }
                    }
                }
                MessageBox.Show("Domain Name " + context.Name);
            }

            return users;
        }
        **/


        // Асинзронный метод поиска пользователей в AD
        public static async Task<List<DomainUser>> FindDomainUserAsync(
            string userToFind,
            string dcName,
            string countName,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var users = new List<DomainUser>();
                int _maxResults = string.IsNullOrEmpty(countName) ? 1 : Convert.ToInt32(countName);
                string _userToFind = string.IsNullOrWhiteSpace(userToFind) ? "*" : userToFind;

                try
                {
                    using (var context = new PrincipalContext(ContextType.Domain, dcName))
                    {
                        // Проверка отмены операции
                        cancellationToken.ThrowIfCancellationRequested();

                        if (!context.ValidateCredentials(null, null, ContextOptions.SimpleBind))
                        {
                            throw new Exception($"Не удалось подключиться к контроллеру домена '{dcName}'");
                        }

                        var userPrincipal = new UserPrincipal(context) { Name = _userToFind };

                        using (var searcher = new PrincipalSearcher(userPrincipal))
                        {
                            var directorySearcher = (DirectorySearcher)searcher.GetUnderlyingSearcher();                            
                            SetDirectorySearcherTimeout(directorySearcher, TimeSpan.FromSeconds(10));// Кросс-платформенная установка таймаута
                            foreach (var result in searcher.FindAll())
                            {
                                cancellationToken.ThrowIfCancellationRequested();// Проверка отмены операции
                                if (users.Count >= _maxResults) break;

                                if (result is UserPrincipal user)
                                {
                                    users.Add(new DomainUser
                                    {
                                        Number = users.Count + 1, //Порядковый номер записи
                                        DomainName = context.Name,
                                        Username = user.SamAccountName,
                                        DisplayName = user.DisplayName,
                                        Email = user.EmailAddress
                                    });
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    MessageBox.Show("Поиск пользователя отменен");
                    return users;  // Операция была отменена - возвращаем частичные результаты
                }
                catch (Exception )
                {
                    // Логирование ошибки
                    //MessageBox.Show($"Ошибка поиска: {ex.Message}");
                    //Debug.WriteLine($"Ошибка поиска: {ex.Message}");
                    throw; // Пробрасываем исключение для обработки вызывающим кодом
                }
                return users;
            }, cancellationToken);
        }

        // Метод для кросс-платформенной установки таймаута
        private static void SetDirectorySearcherTimeout(DirectorySearcher searcher, TimeSpan timeout)
        {
            try
            {
                // Попытка 1: Стандартное свойство .NET Framework
                if (searcher.GetType().GetProperty("Timeout") != null)
                {
                    searcher.GetType().GetProperty("Timeout").SetValue(searcher, timeout);
                    return;
                }

                // Попытка 2: Свойства .NET Core 3.1+
                if (searcher.GetType().GetProperty("ClientTimeout") != null)
                {
                    searcher.GetType().GetProperty("ClientTimeout").SetValue(searcher, timeout);
                }

                if (searcher.GetType().GetProperty("ServerTimeLimit") != null)
                {
                    searcher.GetType().GetProperty("ServerTimeLimit").SetValue(searcher, timeout);
                }
            }
            catch
            {
                // Если не удалось установить таймаут - продолжаем без него
                Debug.WriteLine("Не удалось установить таймаут для DirectorySearcher");
            }
        }

        public static List<DomainUser> FindDomainUser(string userToFind, string dcName, string countName)
        {
            var users = new List<DomainUser>();
            //int _countName = Convert.ToInt32(countName);

            int _maxResults = string.IsNullOrEmpty(countName) ? 1 : Convert.ToInt32(countName);     //Ограничение количества результатов

            string _userToFind = string.IsNullOrWhiteSpace(userToFind) ? "*" : userToFind;

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, dcName))
                {
                    try
                    {
                        if (!context.ValidateCredentials(null, null, ContextOptions.SimpleBind)) // Явная проверка подключения
                        {
                            MessageBox.Show($"Не удалось подключиться к контроллеру домена '{dcName}'", "Ошибка подключения");
                            return users;
                        }

                        var userPrincipal = new UserPrincipal(context)  // Создаем шаблон пользователя для поиска
                        {
                            //SamAccountName = _userToFind, // Ищем по SamAccountName (логин без домена)
                            Name = _userToFind,                               // ИЛИ можно искать по имени (DisplayName) Name = userToFind,
                                                                              // ИЛИ по email EmailAddress = userToFind
                        };

                        using (var searcher = new PrincipalSearcher(userPrincipal))
                        {
                            foreach (var result in searcher.FindAll())
                            {
                                if (users.Count >= _maxResults) break;
                                if (result is UserPrincipal user)
                                {
                                    users.Add(new DomainUser
                                    {
                                        DomainName = context.Name,
                                        Username = user.SamAccountName,
                                        DisplayName = user.DisplayName,
                                        Email = user.EmailAddress
                                    });
                                }
                            }
                        }
                    }
                    catch (PrincipalServerDownException ex)
                    {
                        MessageBox.Show($"Сервер домена '{dcName}' недоступен: {ex.Message}", "Ошибка подключения");
                    }
                    catch (PrincipalOperationException ex)
                    {
                        MessageBox.Show($"Ошибка при работе с доменом: {ex.Message}", "Ошибка операции");
                    }
                }

            }
            catch (ActiveDirectoryObjectNotFoundException ex)
            {
                MessageBox.Show($"Домен '{dcName}' не найден: {ex.Message}", "Домен не найден");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Неожиданная ошибка: {ex.Message}", "Ошибка");
            }

            return users;
        }

        /**
        public static bool AuthenticateDomainUser(string username, string password)
        {
            using (var context = new PrincipalContext(ContextType.Domain))
            {
                return context.ValidateCredentials(username, password);
            }
        }
        **/
    }
}


