using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents.Services.Interface
{
    public interface IFileService
    {
        // Копирование файла с обработкой ошибок
        Task<bool> CopyFileAsync(string sourcePath, string targetPath);

        // Удаление файла с обработкой ошибок
        Task<bool> DeleteFileAsync(string filePath);

        // Создание директории если не существует
        void EnsureDirectoryExists(string directoryPath);

        // Генерация пути для вложений на основе даты события
        string GenerateAttachmentsPath(DateTime eventDate);

        // Проверка существования файла
        bool FileExists(string filePath);

        // Получение информации о файле
        FileInfo GetFileInfo(string filePath);
    }
}
