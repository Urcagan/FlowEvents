using FlowEvents.Models;
using FlowEvents.Repositories.Interface;
using FlowEvents.Services.Interface;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace FlowEvents.Repositories.Implementations
{
    public class AttachFilesRepository : IAttachFilesRepository
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        public AttachFilesRepository(IConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
        }


        // Метод для получения списка вложенных файлов по EventId
        public async Task<IEnumerable<AttachedFileModel>> GetByEventIdAsync(long eventId)
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            var files = new List<AttachedFileModel>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                const string query = "SELECT * FROM AttachedFiles WHERE EventId = @EventId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EventId", eventId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var file = MapReaderToAttachedFileModel(reader, connectionString);
                            files.Add(file);
                        }
                    }
                }
            }

            return files;
        }

        // Метод для получения файла по его ID
        public async Task<AttachedFileModel> GetByIdAsync(int fileId)
        {
            var connectionString = _connectionStringProvider.GetConnectionString();

            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                const string query = "SELECT * FROM AttachedFiles WHERE FileId = @FileId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FileId", fileId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapReaderToAttachedFileModel(reader, connectionString);
                        }
                    }
                }
            }

            return null;
        }


        // Сохранение информации о вложенных файлах в БД
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

        // Обновление информации о вложенном файле
        public async Task<bool> UpdateAsync(AttachedFileModel file)
        {
            var connectionString = _connectionStringProvider.GetConnectionString();

            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                const string query = @"
                UPDATE AttachedFiles SET
                    FileCategory = @FileCategory,
                    FileName = @FileName,
                    FilePath = @FilePath,
                    FileSize = @FileSize,
                    FileType = @FileType,
                    UploadDate = @UploadDate
                WHERE FileId = @FileId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FileId", file.FileId);
                    command.Parameters.AddWithValue("@FileCategory", file.FileCategory);
                    command.Parameters.AddWithValue("@FileName", file.FileName);
                    command.Parameters.AddWithValue("@FilePath", file.FilePath);
                    command.Parameters.AddWithValue("@FileSize", file.FileSize);
                    command.Parameters.AddWithValue("@FileType", file.FileType);
                    command.Parameters.AddWithValue("@UploadDate", file.UploadDate.ToString("yyyy-MM-dd HH:mm:ss"));

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        // Удаление записи о подключенном файле по его ID
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


        // Удаление всех записей о подключенных файлах для данного события
        public async Task<bool> DeleteByEventIdAsync(long eventId)
        {
            var connectionString = _connectionStringProvider.GetConnectionString();

            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                const string query = "DELETE FROM AttachedFiles WHERE EventId = @EventId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EventId", eventId);

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        private AttachedFileModel MapReaderToAttachedFileModel(SQLiteDataReader reader, string connectionString)
        {
            return new AttachedFileModel()
            {
                FileId = reader.GetInt32(reader.GetOrdinal("FileId")),
                EventId = reader.GetInt32(reader.GetOrdinal("EventId")),
                FileCategory = reader.GetString(reader.GetOrdinal("FileCategory")),
                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                UploadDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("UploadDate"))),
                FileType = reader.GetString(reader.GetOrdinal("FileType"))

                // FileId = Convert.ToInt32(reader["FileId"]),
                //EventId = Convert.ToInt32(reader["EventId"]),
                //FileCategory = reader["FileCategory"].ToString(),
                //FileName = reader["FileName"].ToString(),
                //FilePath = reader["FilePath"].ToString(),
                //FileSize = Convert.ToInt64(reader["FileSize"]),
                //UploadDate = DateTime.Parse(reader["UploadDate"].ToString()),
                //FileType = reader["FileType"].ToString()
            };
        }
    }
}
