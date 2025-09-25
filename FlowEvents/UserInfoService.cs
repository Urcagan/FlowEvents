using FlowEvents.Models;
using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;

namespace FlowEvents
{
    public class UserInfoService
    {
        
        public static UserInfo GetCurrentUserInfo() // Получение информации о текущем пользователе
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
                        Login = userPrincipal.SamAccountName,   
                        DisplayName = userPrincipal.DisplayName,
                        FullLogin = userPrincipal.UserPrincipalName,
                        Domain = userPrincipal.Context.Name,
                        Email = userPrincipal.EmailAddress,
                        DistinguishedName = userPrincipal.DistinguishedName,
                        IsDomainUser = IsDomainUser(userPrincipal),
                        SID = userPrincipal.Sid?.Value,
                        Description = userPrincipal.Description
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения информации о пользователе: {ex.Message}");
                return GetFallbackUserInfo();                                                       // Возвращаем базовую информацию в случае ошибки
            }
        }

        private static PrincipalContext GetPrincipalContext()       // Получение контекста (доменный или локальный)
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

        private static bool IsDomainEnvironment()
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

        private static bool IsDomainUser(UserPrincipal user)        // Проверяем, является ли пользователь доменным
        {
            return user.Context.ContextType == ContextType.Domain;  // Если контекст доменный, значит пользователь доменный
        }

        private static UserInfo GetFallbackUserInfo()               // Получение базовой информации о пользователе
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
                IsDomainUser = !Environment.UserDomainName.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            };
        }
    }
}
