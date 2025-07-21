using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.Models
{
    public class AttachedFileModel
    {
        private readonly string _connectionString;
        public AttachedFileModel(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int FileId { get; set; }
        public int EventId { get; set; }  // Связь с таблицей Events
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public DateTime UploadDate { get; set; }

        //public EventAddViewModel EventAddViewModel { get; set; }

        public event Action<AttachedFileModel> FileDeleted; // Событие, которое будет вызываться при удалении файла

        public ICommand OpenCommand => new RelayCommand(_ => OpenFile()); // Команда для открытия файла

        public ICommand DeleteCommand => new RelayCommand(_ => DeleteFile()); // Команда для удаления файла

        private void OpenFile()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть файл: {ex.Message}");
            }
        }

        private void DeleteFile()
        {
            if (MessageBox.Show("Удалить файл?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return; // Если пользователь отменил действие, выходим из метода
            }

            try
            {
                // 1. Удаление файла с диска
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
                // 2. Удаляем запись из БД (если FileId задан)
                if (FileId > 0)
                {
                    using (var connection = new SQLiteConnection(_connectionString)) // Используем переданную строку
                    {
                        connection.Open();
                        string query = "DELETE FROM AttachedFiles WHERE FileId = @FileId";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@FileId", FileId);
                            command.ExecuteNonQuery();
                        }
                    }
                }

                // 3. Вызываем событие, чтобы уведомить ViewModel
                FileDeleted?.Invoke(this);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить файл: {ex.Message}");
            }
        }
    }
}
