using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class DatabaseValidationService : IDatabaseValidationService
    {
        private readonly IDatabaseInfoRepository _databaseRepository;

        public DatabaseValidationService(IDatabaseInfoRepository databaseRepository)
        {
            _databaseRepository = databaseRepository;
        }


        // Новый метод для валидации с путем и версией
        public async Task<DatabaseValidationResult> ValidateDatabaseAsync(string databasePath, string expectedVersion)
        {
            var databaseInfo = new DatabaseInfo
            {
                Path = databasePath,
                Version = expectedVersion
            };

            return await ValidateDatabaseAsync(databaseInfo); // Вызов основного метода валидации
        }


        // Метод для валидации базы данных по объекту DatabaseInfo
        public async Task<DatabaseValidationResult> ValidateDatabaseAsync(DatabaseInfo databaseInfo)
        {
            // 1. Проверка файла
            if (!ValidateFile(databaseInfo.Path)) // Если файл не найден
            {
                return new DatabaseValidationResult
                {
                    IsValid = false,
                    Message = $"Файл базы данных не найден: {databaseInfo.Path}",
                    ErrorType = ValidationErrorType.FileNotFound,
                    DatabaseInfo = databaseInfo

                };
            }

            databaseInfo.Exists = true; // Выставляем состояние существования файла базы данных

            var connectionString = CreateConnectionString(databaseInfo.Path); // Создание строки подключения

            // 2. Проверка подключения
            if (!await _databaseRepository.TestConnectionAsync(connectionString)) // Если не удалось подключиться
            {
                return new DatabaseValidationResult
                {
                    IsValid = false,
                    Message = "Не удалось подключиться к базе данных",
                    ErrorType = ValidationErrorType.ConnectionError,
                    DatabaseInfo = databaseInfo
                };
            }

            // 3. Проверка структуры
            if (!await _databaseRepository.CheckDatabaseStructureAsync(connectionString)) // Если структура неверная
            {
                return new DatabaseValidationResult
                {
                    IsValid = false,
                    Message = "База данных имеет неверную структуру",
                    ErrorType = ValidationErrorType.InvalidStructure,
                    DatabaseInfo = databaseInfo
                };
            }

            // 4. Проверка версии
            var actualVersion = await _databaseRepository.GetDatabaseVersionAsync(connectionString); // Получение текущей версии из БД
            if (actualVersion != databaseInfo.Version) // Если версия не совпадает
            {
                return new DatabaseValidationResult
                {
                    IsValid = false,
                    Message = $"Версия БД ({actualVersion}) не соответствует требуемой ({databaseInfo.Version})",
                    ErrorType = ValidationErrorType.InvalidVersion,
                    DatabaseInfo = databaseInfo
                };
            }

            // если прошли все проверки и база валидна то заполняем отчет
            databaseInfo.Exists = true;
            databaseInfo.IsValid = true;
            databaseInfo.Version = actualVersion;

            return new DatabaseValidationResult
            {
                IsValid = true,
                Message = "База данных прошла проверку успешно",
                DatabaseInfo = databaseInfo,
                ConnectionString = connectionString
            };
        }

        public bool ValidateFile(string databasePath)
        {
            return !string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath);
        }

        private string CreateConnectionString(string databasePath)
        {
            return $"Data Source={databasePath};Version=3;foreign keys=true;";
        }
    }
}
