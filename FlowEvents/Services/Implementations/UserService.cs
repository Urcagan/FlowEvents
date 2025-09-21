using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<(bool Success, string Message)> RegisterUserAsync(string username, string password, string confirmPassword, int DefaultRoleID)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
                return (false, "Пожалуйста, заполните все поля");

            if (password != confirmPassword)
                return (false, "Пароли не совпадают");

            if (password.Length < 6)
                return (false, "Пароль должен содержать минимум 6 символов");

            try
            {
                // Проверка существования пользователя
                if (await _userRepository.UserExistsAsync(username))
                    return (false, $"Пользователь '{username}' уже существует");

                // Генерация соли и хеша
                var salt = _passwordHasher.GenerateSalt();                          // Генерация соли
                var hashedPassword = _passwordHasher.HashPassword(password, salt);  // Хеширование пароля с солью

                // Добавление пользователя
                var userId = await _userRepository.AddUserAsync(username, hashedPassword, salt, DefaultRoleID); 

                return (true, $"Пользователь {username} успешно зарегистрирован"); 
            }
            catch (SQLiteException ex)
            {
                return (false, $"Ошибка базы данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка: {ex.Message}");
            }
        }


        //public async Task ChangeUserRoleAsync(int userId, int newRoleId, int changedByUserId)
        public async Task ChangeUserRoleAsync(string userName, int newRoleId)
        {
            // 1. Проверка существования роли
            var roleExists = await _roleRepository.RoleExistsAsync(newRoleId);
            if (!roleExists)
                throw new Exception("Роль не существует");

            // 3. Проверка нельзя лишить себя прав
            //if (userId == changedByUserId)
            //    throw new BusinessException("Нельзя изменить роль самому себе");

            // 4. Обновление роли
            await _userRepository.UpdateUserRoleAsync(userName, newRoleId);
        }


        public async Task<List<User>> GetUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }
    }
}
