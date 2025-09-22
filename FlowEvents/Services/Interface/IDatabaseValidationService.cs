using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Interface
{
    public interface IDatabaseValidationService
    {
        Task<DatabaseValidationResult> ValidateDatabaseAsync(string databasePath, string expectedVersion); // Новый метод для валидации с путем и версией
        Task<DatabaseValidationResult> ValidateDatabaseAsync(DatabaseInfo databaseInfo); // Метод для валидации базы данных по объекту DatabaseInfo
        bool ValidateFile(string databasePath); // Метод для проверки наличия файла базы данных
    }

    public class DatabaseValidationResult // Результат валидации базы данных
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }                 // Текст сообщения ошибки
        public ValidationErrorType ErrorType { get; set; }  // Тип ошибки
        public DatabaseInfo DatabaseInfo { get; set; }      // информационная структура о базе данных
        public string ConnectionString { get; set; }        // Строка подключения к базе данных
    }
}
