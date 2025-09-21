using FlowEvents.Services.Interface;
using System;
using System.Security.Cryptography;
using System.Text;

namespace FlowEvents.Services.Implementations
{
    // Сервис для хэширования паролей
    public class PasswordHasher : IPasswordHasher
    {
        public string GenerateSalt()    // Используем GUID для генерации соли
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        public string HashPassword(string password, string salt)  // Хэшируем пароль с использованием SHA256 и соли
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var bytes = Encoding.UTF8.GetBytes(saltedPassword);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
