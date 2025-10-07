using System;

namespace FlowEvents.Models
{
    public class UserInfo
    {
        public string Login { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FullLogin { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;
        public bool IsDomainUser { get; set; }
        public string SID { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserType => IsDomainUser ? "Доменный пользователь" : "Локальный пользователь";
        
        
        public void PrintInfo()
        {
            Console.WriteLine("=== ИНФОРМАЦИЯ О ПОЛЬЗОВАТЕЛЕ ===");
            Console.WriteLine($"Логин: {Login}");
            Console.WriteLine($"Отображаемое имя: {DisplayName}");
            Console.WriteLine($"Полный логин: {FullLogin}");
            Console.WriteLine($"Домен: {Domain}");
            Console.WriteLine($"Email: {Email ?? "Не указан"}");
            Console.WriteLine($"Доменный пользователь: {IsDomainUser}");
            Console.WriteLine($"SID: {SID}");
            Console.WriteLine($"Описание: {Description ?? "Не указано"}");
            Console.WriteLine("=================================");
        }
    }
}
