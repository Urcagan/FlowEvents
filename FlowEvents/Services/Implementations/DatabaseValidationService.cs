using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace FlowEvents.Services.Implementations
{
    public class DatabaseValidationService : IDatabaseValidationService
    {
        private readonly IDatabaseInfoRepository _databaseRepository;
        private readonly IConnectionStringProvider _connectionStringProvider;

        public DatabaseValidationService(IDatabaseInfoRepository databaseRepository, IConnectionStringProvider connectionStringProvider)
        {
            _databaseRepository = databaseRepository;
            _connectionStringProvider = connectionStringProvider;
        }


        // Новый метод для валидации с путем и версией
        public async Task<DatabaseValidationResult> ValidateDatabaseAsync(string databasePath, int expectedVersion)
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

            // 1. Проверка файла на существование по указанному пути
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

            // Для сетевого расположения файла необходимо модифицыровать к пуи раложения файла добаавлять еще 4 слеша "\\\\" 


            databaseInfo.Exists = true; // Выставляем состояние существования файла базы данных

            var connectionString = _connectionStringProvider.CreateConnectionString(databaseInfo.Path); // Создание строки подключения

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
            var actualVersion = await _databaseRepository.GetUserVersion(connectionString); //.GetDatabaseVersionAsync(connectionString); // Получение текущей версии из БД
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
            // Проверка наличия файла по указанному пути
            return !string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath);
        }

    }
}
