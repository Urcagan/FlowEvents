using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Models
{
    public class DatabaseInfo
    {
        public string Path { get; set; }            // Путь к базе данных
        public string Version { get; set; }         // Версия базы данных
        public bool Exists { get; set; }            // Файл базы данных существует
        public bool IsValid { get; set; }           // Состояние валидности базы данных
    }

    public enum ValidationErrorType // Типы ошибомалидации базы днных
    {
        None = 0,
        FileNotFound,       // Файл не найден
        InvalidVersion,     // Ошибка версии базы 
        ConnectionError,    // Ошибка подключения к базе данных
        InvalidStructure    // Ошибка структуры базы данных
    }
}
