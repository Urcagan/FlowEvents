using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Windows;

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


        public async Task<(bool Success, string Message)> RegisterDomainUserAsync(string username, string domainName, string displayName, string email, int DefaultRoleID)
        {
            if (string.IsNullOrWhiteSpace(username) )
                return (false, "Пожалуйста, заполните все поля");
            try
            {
                // Проверка существования пользователя
                if (await _userRepository.UserExistsAsync(username))
                    return (false, $"Пользователь '{username}' уже существует");

                // Добавление пользователя
                var userId = await _userRepository.AddDomenUserAsync(username, domainName, displayName, email, DefaultRoleID);

                return (true, $"Пользователь {username} успешно добавлен");
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

        // public async Task<bool> DeleteUserAsync(string userName, string currentUser)
        public async Task<bool> DeleteUserAsync(string userName)
        {
            // Проверка: нельзя удалить самого себя
            //if (userName.Equals(currentUser, StringComparison.OrdinalIgnoreCase))
            //{
            //    throw new InvalidOperationException("Нельзя удалить собственный аккаунт");
            //}

            // Проверка существования пользователя
            if (!await _userRepository.UserExistsAsync(userName))
            {
                throw new ArgumentException($"Пользователь '{userName}' не существует");
            }

            // Удаление пользователя
            return await _userRepository.DeleteUserAsync(userName);
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

        public async Task UpdateUserAccessAsync(string username, bool isAllowed)
        {
            await _userRepository.UpdateUserAccessAsync(username, isAllowed);
        }


        public async Task<List<User>> GetUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }
    }
}
