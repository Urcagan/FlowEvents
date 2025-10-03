using FlowEvents.Services.Interface;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents.Services.Implementations
{
    public class FileService : IFileService
    {
        public async Task<bool> CopyFileAsync(string sourcePath, string targetPath)
        {
            try
            {
                // Создаем директорию если не существует
                EnsureDirectoryExists(Path.GetDirectoryName(targetPath));

                // Используем асинхронное копирование
                using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                using (var destinationStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования файла: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    return true;
                }
                return true; // Файл уже не существует
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления файла: {ex.Message}");
                return false;
            }
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания директории: {ex.Message}");
                throw;
            }
        }

        public string GenerateAttachmentsPath(DateTime eventDate)
        {
            try
            {
                string filePath = Global_Var.pathToDB;
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

                string basePath = Path.GetDirectoryName(filePath);
                string attachmentsRoot = "Attachments";
                string yearFolder = eventDate.Year.ToString();
                string monthFolder = eventDate.Month.ToString("00");

                return Path.Combine(basePath, attachmentsRoot, yearFolder, monthFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации пути: {ex.Message}");
                throw;
            }
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public FileInfo GetFileInfo(string filePath)
        {
            return new FileInfo(filePath);
        }
    }
}
