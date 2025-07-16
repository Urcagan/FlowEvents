using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FlowEvents.Models
{
    public class AttachedFileModel
    {
        public int FileId { get; set; }
        public int EventId { get; set; }  // Связь с таблицей Events
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public DateTime UploadDate { get; set; }

        public ICommand OpenCommand => new RelayCommand(_ => OpenFile());
        public ICommand DeleteCommand => new RelayCommand(_ => DeleteFile());

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
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                    // Здесь нужно также удалить запись из БД
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить файл: {ex.Message}");
            }
        }
    }
}
