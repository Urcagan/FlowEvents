using FlowEvents.Models;
using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace FlowEvents.Services.Implementations
{
    public class FileAttachmentService
    {
        private readonly string _storageRoot;
        private readonly string _connectionString;

        public FileAttachmentService(string storageRoot, string connectionString)
        {
            _storageRoot = storageRoot;
            _connectionString = connectionString;
        }

        public async Task<bool> AttachFileToRecordAsync(AttachedFileModel file, Stream fileStream)
        {
            // 1. Генерация пути
            string fileExtension = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            string yearMonth = DateTime.Now.ToString("yyyy\\MM");
            string relativePath = Path.Combine(yearMonth, uniqueFileName);
            string fullPath = Path.Combine(_storageRoot, relativePath);

            // 2. Сохранение на диск
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            // 3. Запись в SQLite
            string query = @"
                INSERT INTO AttachedFiles 
                (RecordId, FileName, FilePath, FileSize, FileType, Description, UploadedBy)
                VALUES 
                (@RecordId, @FileName, @FilePath, @FileSize, @FileType, @Description, @UploadedBy)";

            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RecordId", file.EventId);
                    command.Parameters.AddWithValue("@FileName", file.FileName);
                    command.Parameters.AddWithValue("@FilePath", relativePath);
                    command.Parameters.AddWithValue("@FileSize", new FileInfo(fullPath).Length);
                    command.Parameters.AddWithValue("@FileType", fileExtension);
                    //command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(file.Description) ? DBNull.Value : (object)file.Description);
                    command.Parameters.AddWithValue("@UploadedBy", Environment.UserName);

                    await command.ExecuteNonQueryAsync();
                }
            }

            return true;
        }
    }
}
