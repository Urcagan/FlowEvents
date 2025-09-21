namespace FlowEvents.Services.Interface
{
    // Сервис для хеширования паролей
    public interface IPasswordHasher
    {
        string GenerateSalt(); // Метод для генерации соли
        string HashPassword(string password, string salt); // Метод для хеширования пароля с использованием соли
    }
}
