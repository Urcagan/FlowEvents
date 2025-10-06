using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents.Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly IAttachFilesRepository _attachFilesRepository;

        public FileService(IAttachFilesRepository attachFilesRepository)
        {
            _attachFilesRepository = attachFilesRepository;
        }


       public async Task SaveAttachedFilesToDatabase(long eventId, IEnumerable<AttachedFileModel> attachedFiles)
        {
            await _attachFilesRepository.InsertEventAttachmentsAsync(eventId, attachedFiles);
        }


        public async Task<bool> CopyFileAsync(string sourcePath, string targetPath) // Асинхронное копирование файла с обработкой ошибок
        {
            try
            {
                EnsureDirectoryExists(Path.GetDirectoryName(targetPath)); // Создаем директорию если не существует

                // Используем асинхронное копирование
                // 1. Создаются два файловых потока
                // 2. Данные читаются из исходного файла и пишутся в целевой
                // 3. При завершении операции ресурсы автоматически освобождаются
                //Это эффективный способ копирования файлов с поддержкой асинхронных операций.
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


        // Удаление файла из БД и HDD  
        public async Task DeleteFileWithConfirmation(int fileId, string filePath)
        {
            // Сначала удаляем из БД
            bool isDeletedFromDb = await _attachFilesRepository.DeleteAsync(fileId);

            if (isDeletedFromDb)
            {
                // Только если удаление из БД успешно - удаляем физический файл
                await DeleteFileAsync(filePath);

            }
            else
            {
                throw new InvalidOperationException($"Файд с ID {fileId} не найден в базе данных");
            }
        }
    }
}
