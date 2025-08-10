using FlowEvents.Models.Enums;
using System;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.Models
{
    public class AttachedFileModel : INotifyPropertyChanged
    {
        private readonly string _connectionString;
        public AttachedFileModel(string connectionString)
        {
            _connectionString = connectionString;
        }

        private FileStatus _status; // Статус файла (новый, существующий, изменённый, удалённый)
                                    
        public string SourceFilePath { get; set; } // Путь к исходному файлу, если требуется
        public int FileId { get; set; }
        public int EventId { get; set; }  // Связь с таблицей Events
        public string FileCategory { get; set; } // Тип прикрепляемого документа
        public string FileName { get; set; } // Имя файла, которое будет отображаться в UI
        public string FilePath { get; set; } // Путь к файлу на диске, где он хранится
        public long FileSize { get; set; } // Размер файла в байтах
        public string FileType { get; set; } // Тип файла (расширение, например .jpg, .pdf и т.д.)
        public DateTime UploadDate { get; set; } // Дата загрузки файла

        public event Action<AttachedFileModel> FileDeleted; // Событие, которое будет вызываться при удалении файла

        //public EventAddViewModel EventAddViewModel { get; set; }

        
        public FileStatus Status  // Свойство для доступа к статусу
        {
            get => _status;
            private set  // Запрещаем изменение статуса напрямую
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }
        // Методы для изменения статуса
        public void MarkAsNwe() => Status = FileStatus.New;
        public void MarkAsExisting() => Status = FileStatus.Existing;
        public void MarkAsModified() => Status = FileStatus.Modified;
        public void MarkAsDeleted() => Status = FileStatus.Deleted;

        private FileStatus _postStatus; // Хранение предыдущего статуса файла

        // Команды для взаимодействия с файлом
        public ICommand OpenCommand => new RelayCommand(_ => OpenFile()); // Команда для открытия файла

        public ICommand DeleteCommand => new RelayCommand(_ => DeleteFile()); // Команда для удаления файла

        public ICommand ToggleDeleteCommand => new RelayCommand(_ => ToggleDelete());


        // Конструктор по умолчанию для сериализации
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

        // Метод для удаления файла
        private void DeleteFile()
        {
            if (MessageBox.Show("Удалить файл?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return; // Если пользователь отменил действие, выходим из метода
            }

            //try
            //{
            //    // 1. Удаление файла с диска
            //    if (File.Exists(FilePath))
            //    {
            //        File.Delete(FilePath);
            //    }
            //    // 2. Удаляем запись из БД (если FileId задан)
            //    if (FileId > 0)
            //    {
            //        using (var connection = new SQLiteConnection(_connectionString)) // Используем переданную строку
            //        {
            //            connection.Open();
            //            string query = "DELETE FROM AttachedFiles WHERE FileId = @FileId";
            //            using (var command = new SQLiteCommand(query, connection))
            //            {
            //                command.Parameters.AddWithValue("@FileId", FileId);
            //                command.ExecuteNonQuery();
            //            }
            //        }
            //    }

            //    // 3. Вызываем событие, чтобы уведомить ViewModel
                FileDeleted?.Invoke(this);

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Не удалось удалить файл: {ex.Message}");
            //}
        }

        // Отмена удаления файла
        private void ToggleDelete()
        {
            if (Status == FileStatus.Deleted)
            {
                Status = _postStatus; // Или другой подходящий статус
            }
            else
            {
                _postStatus = Status; // Сохраняем предыдущий статус файла
                Status = FileStatus.Deleted;
            }

            // Уведомляем об изменении
            FileDeleted?.Invoke(this);
        }

        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для генерации события
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}


namespace FlowEvents.Models.Enums
{
    // Перечисление для статуса файла
    public enum FileStatus
    {
        New,        // Файл только что добавлен (ещё не сохранён в БД)
        Existing,   // Файл уже сохранён в БД
        Modified,   // Файл изменён (например, переименован)
        Deleted     // Файл помечен на удаление
    }

    public enum FileCategory
    {
        document,
        monitoring
    }
}


public class AttachedFileForEvent
{   
    public int FileId { get; set; }
    public int EventId { get; set; }  // Связь с таблицей Events
    public string FileCategory { get; set; } // Тип прикрепляемого документа
    public string FileName { get; set; } // Имя файла, которое будет отображаться в UI
    public string FilePath { get; set; } // Путь к файлу на диске, где он хранится
}
