using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class AttachFilesRepository : IAttachFilesRepository
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        public AttachFilesRepository( IConnectionStringProvider connectionStringProvider) 
        {
            _connectionStringProvider = connectionStringProvider;
        }


        public async Task InsertEventAttachmentsAsync(long eventId, IEnumerable<AttachedFileModel> attachedFiles)
        {
            // Проверка на пустую коллекцию
            if (!attachedFiles.Any())
                return;

            var connectionString = _connectionStringProvider.GetConnectionString();

            using var connection = new SQLiteConnection(connectionString);
            await connection.OpenAsync();

            // Используем транзакцию для атомарности
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var file in attachedFiles)
                {
                    const string query = @"
                                        INSERT INTO AttachedFiles 
                                        (EventId, FileCategory, FileName, FilePath, FileSize, FileType, UploadDate)
                                        VALUES (@EventId, @FileCategory, @FileName, @FilePath, @FileSize, @FileType, @UploadDate)";

                    using var command = new SQLiteCommand(query, connection, transaction);

                    command.Parameters.AddWithValue("@EventId", eventId);
                    command.Parameters.AddWithValue("@FileCategory", file.FileCategory);
                    command.Parameters.AddWithValue("@FileName", file.FileName);
                    command.Parameters.AddWithValue("@FilePath", file.FilePath);
                    command.Parameters.AddWithValue("@FileSize", file.FileSize);
                    command.Parameters.AddWithValue("@FileType", file.FileType);
                    command.Parameters.AddWithValue("@UploadDate", file.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"));

                    await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw; // Перебрасываем исключение выше
            }
        }


        // Удаление записи о подключенном файле 
        public async Task<bool> DeleteAsync(int fileId) 
        {
            var _connectionString = _connectionStringProvider.GetConnectionString();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                string query = "DELETE FROM AttachedFiles WHERE FileId = @id";
                await connection.OpenAsync();
                var cmd = new SQLiteCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", fileId);
                int affectedRows = await cmd.ExecuteNonQueryAsync();

                return affectedRows > 0; // true если запись удалена
            }
        }
    }
}
