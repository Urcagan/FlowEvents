using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;

namespace FlowEvents
{
    public class DomainHelper
    {
        public static List<DomainUser> GetDomainUsers()
        {
            var users = new List<DomainUser>();

            using (var context = new PrincipalContext(ContextType.Domain))
            {
                using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                {
                    foreach (var result in searcher.FindAll())
                    {
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

            return users;
        }

        public static bool AuthenticateDomainUser(string username, string password)
        {
            using (var context = new PrincipalContext(ContextType.Domain))
            {
                return context.ValidateCredentials(username, password);
            }
        }
    }

    public class DomainUser
    {
        public string DomainName { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
    }
}
