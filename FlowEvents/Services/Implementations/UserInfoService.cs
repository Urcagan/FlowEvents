using FlowEvents.Models;
using FlowEvents.Services.Interface;
using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;

namespace FlowEvents
{
    public class UserInfoService : IUserInfoService
    {
        public UserInfo GetCurrentUserInfo() // Получение информации о текущем пользователе
        {
            try
            {
                using (var context = GetPrincipalContext())             // Получаем контекст (доменный или локальный)
                using (var userPrincipal = UserPrincipal.Current)       // Получаем текущего пользователя
                {
                    if (userPrincipal == null)                          // Если не удалось получить информацию о пользователе
                    {
                        return GetFallbackUserInfo();                   // Возвращаем базовую информацию
                    }

                    return new UserInfo     // Заполняем информацию о пользователе
                    {
                        Login = userPrincipal.SamAccountName ?? Environment.UserName,
                        DisplayName = userPrincipal.DisplayName ?? Environment.UserName,
                        FullLogin = userPrincipal.UserPrincipalName ?? WindowsIdentity.GetCurrent().Name,
                        Domain = userPrincipal.Context.Name ?? Environment.UserDomainName,
                        Email = userPrincipal.EmailAddress ?? "Не указан",
                        DistinguishedName = userPrincipal.DistinguishedName ?? "Не доступно",
                        IsDomainUser = userPrincipal.Context.ContextType == ContextType.Domain,
                        SID = userPrincipal.Sid?.Value ?? "Не доступно",
                        Description = userPrincipal.Description ?? "Не указано"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения информации о пользователе: {ex.Message}");
                return GetFallbackUserInfo();                                                       // Возвращаем базовую информацию в случае ошибки
            }
        }

        private PrincipalContext GetPrincipalContext()       // Получение контекста (доменный или локальный)
        {
            // Автоматически определяем контекст
            if (IsDomainEnvironment())
            {
                return new PrincipalContext(ContextType.Domain);
            }
            else
            {
                return new PrincipalContext(ContextType.Machine);
            }
        }

        private bool IsDomainEnvironment()
        {
            try
            {
                using (var domainContext = new PrincipalContext(ContextType.Domain))  // Проверяем доступность доменного контекста
                {
                    return domainContext.ConnectedServer != null;                   // Если сервер доступен, значит мы в доменной среде
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsDomainUser(UserPrincipal user)        // Проверяем, является ли пользователь доменным
        {
            return user.Context.ContextType == ContextType.Domain;  // Если контекст доменный, значит пользователь доменный
        }

        private UserInfo GetFallbackUserInfo()               // Получение базовой информации о пользователе
        {
            // Fallback на базовые методы, если UserPrincipal недоступен
            var identity = WindowsIdentity.GetCurrent();
            string fullLogin = identity.Name;
            string[] parts = fullLogin.Split('\\');

            return new UserInfo
            {
                Login = Environment.UserName,
                DisplayName = Environment.UserName,
                FullLogin = fullLogin,
                Domain = parts.Length > 1 ? parts[0] : Environment.MachineName,
                Email = "Не доступно",
                DistinguishedName = "Не доступно",
                IsDomainUser = !Environment.UserDomainName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase),
                SID = identity.User?.Value ?? "Не доступно",
                Description = "Информация ограничена"
            };
        }
    }
}
