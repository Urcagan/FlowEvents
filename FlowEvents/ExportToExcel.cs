using ClosedXML.Excel;
using FlowEvents.Models;
using Microsoft.Win32;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace FlowEvents
{
    public class ExportToExcel
    {
        private IEnumerable<EventForView> _events;

        public ExportToExcel(IEnumerable<EventForView> Events)
        {
            _events = Events ?? throw new ArgumentNullException(nameof(Events), "Список событий не может быть null");
        }


        public void ExportToExcelWithDialog()
        {
            try
            {
                // Проверка наличия данных перед экспортом
                if (_events == null || !_events.Any())
                {
                    MessageBox.Show("Нет данных для экспорта.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    FilterIndex = 1,
                    FileName = $"Events_{DateTime.Now:yyyy-MM-dd}.xlsx",
                    OverwritePrompt = true,
                    CheckPathExists = true
                };
                   
                if (saveFileDialog.ShowDialog() == true)
                {
                    var result = ExportToExcelWithClosedXML(_events, saveFileDialog.FileName);

                    if (result.IsSuccess)
                    {
                        MessageBox.Show($"Данные успешно экспортированы в Excel!\n\nФайл: {Path.GetFileName(result.FilePath)}\nЭкспортировано записей: {result.ExportedRecords}",
                                      "Успешный экспорт",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        if (App.Settings.AutoOpenExcel) // Проверяем автоматическое открытие файла 
                        {
                            OpenExcelFileSimple(result.FilePath); // Открываем файл 
                        }

                    }
                    else
                    {
                        MessageBox.Show($"Ошибка при экспорте:\n{result.ErrorMessage}",
                                      "Ошибка экспорта",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }                
            }
            catch (Exception ex)
            {
                HandleUnexpectedError(ex);
            }

        }
    
        private ExportResult ExportToExcelWithClosedXML(IEnumerable<EventForView> events, string filePath)
        {
            var result = new ExportResult { FilePath = filePath };

            try
            {
                // Валидация входных параметров
                ValidateExportParameters(events, filePath);

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Events");

                    // Создание заголовков
                    CreateWorksheetHeaders(worksheet);

                    // Заполнение данными
                    result.ExportedRecords = FillWorksheetWithData(worksheet, events);

                    // Форматирование таблицы
                    FormatWorksheet(worksheet, result.ExportedRecords);

                    // Сохранение файла
                    SaveWorkbookToFile(workbook, filePath);
                }

                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = GetUserErrorMessage(ex);

                // Логирование для разработчика
                LogError(ex, events?.Count() ?? 0, filePath);
            }

            return result;
        }


        #region Вспомогательные методы

        private void ValidateExportParameters(IEnumerable<EventForView> events, string filePath)    // Валидация входных параметров
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events), "Список событий не может быть null");

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым", nameof(filePath));

            if (!events.Any())
                throw new InvalidOperationException("Нет данных для экспорта");

            // Проверка допустимого расширения файла
            var extension = Path.GetExtension(filePath);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Файл должен иметь расширение .xlsx", nameof(filePath));

            // Проверка доступности директории
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    throw new IOException($"Невозможно найти директорию: {directory}", ex);
                }
            }
        }

        private void CreateWorksheetHeaders(IXLWorksheet worksheet) // Создание заголовков
        {
            var headers = new[]
            {
            "Дата", "Установка", "Переработка нефти", "Вид события", "Событие",
            "Примечание, пояснение", "Дата создания", "Автор"
        };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
        }

        private int FillWorksheetWithData(IXLWorksheet worksheet, IEnumerable<EventForView> events) // Заполнение данными
        {
            int row = 2;
            int successCount = 0;

            foreach (var eventItem in events)
            {
                try
                {
                    // Безопасное присвоение значений
                    worksheet.Cell(row, 1).Value = eventItem.DateEvent;
                    worksheet.Cell(row, 2).Value = eventItem.Unit ?? string.Empty;
                    worksheet.Cell(row, 3).Value = eventItem.OilRefining ?? string.Empty;
                    worksheet.Cell(row, 4).Value = eventItem.Category ?? string.Empty;
                    worksheet.Cell(row, 5).Value = eventItem.Description ?? string.Empty;
                    worksheet.Cell(row, 6).Value = eventItem.Action ?? string.Empty;
                    worksheet.Cell(row, 7).Value = eventItem.DateCreate;
                    worksheet.Cell(row, 8).Value = eventItem.Creator ?? string.Empty;

                    successCount++;
                    row++;
                }
                catch (Exception rowEx)
                {
                    // Логируем ошибку для конкретной строки, но продолжаем обработку
                    System.Diagnostics.Debug.WriteLine($"Ошибка при обработке строки {row}: {rowEx.Message}");
                    // Можно также увеличивать счетчик ошибок
                }
            }

            return successCount;
        }

        private object GetSafeDateTimeValue(DateTime? dateTime)
        {
            return dateTime?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
        }

        private void FormatWorksheet(IXLWorksheet worksheet, int dataRowsCount)     // Форматирование таблицы
        {
            if (dataRowsCount > 0)
            {
                // Автоподбор ширины колонок
                worksheet.Columns().AdjustToContents();
                                
                //Настройка строки заголовка
                var dataRange = worksheet.Range(1, 1, 1, 8);
                dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Добавляем границы для всей таблицы
                dataRange = worksheet.Range(1, 1, dataRowsCount + 1, 8);
                
                dataRange.Style.Alignment.WrapText = true;
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

                // Настройкак рамок 
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                
            }
        }

        private void SaveWorkbookToFile(XLWorkbook workbook, string filePath)
        {
            try
            {
                // Проверяем, не занят ли файл другим процессом
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (IOException)
                    {
                        throw new IOException($"Файл {filePath} занят другим процессом. Закройте файл и попробуйте снова.");
                    }
                }

                workbook.SaveAs(filePath);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"Нет прав для записи в файл: {filePath}");
            }
        }   // Сохранение файла

        private string GetUserErrorMessage(Exception ex)
        {
            switch (ex)
            {
                case IOException _:
                    return "Ошибка доступа к файлу. Убедитесь, что файл не открыт в другой программе и у вас есть права на запись.";
                case UnauthorizedAccessException _:
                    return "Отсутствуют права для сохранения файла в выбранную директорию.";
                case ArgumentException _:
                    return "Указан некорректный путь или параметры экспорта.";
                case System.Runtime.InteropServices.COMException _:
                    return "Ошибка взаимодействия с Excel. Убедитесь, что Excel правильно установлен.";
                case InvalidOperationException _:
                    return ex.Message;
                default:
                    return $"Произошла непредвиденная ошибка: {ex.Message}";
            }
        }

        private void LogError(Exception ex, int eventsCount, string filePath)   // Логирование для разработчика
        {
            // Здесь можно добавить логирование в файл, базу данных и т.д.
            System.Diagnostics.Debug.WriteLine(
                "Ошибка экспорта в Excel:\n" +
                $"Время: {DateTime.Now}\n" +
                $"Файл: {filePath}\n" +
                $"Количество событий: {eventsCount}\n" +
                $"Ошибка: {ex.Message}\n" +
                $"Тип: {ex.GetType()}\n" +
                $"StackTrace: {ex.StackTrace}"
            );
        }

        #endregion

        private void HandleUnexpectedError(Exception ex)
        {
            MessageBox.Show($"Произошла непредвиденная ошибка:\n{ex.Message}\n\nПожалуйста, обратитесь к администратору.",
                           "Критическая ошибка",
                           MessageBoxButton.OK, MessageBoxImage.Error);

        }

        #region Вспомогательные классы

        // Открытие файла в Excel
        public void OpenExcelFileSimple(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл не найден!");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Использует ассоциации файлов Windows
                });

                // Или просто:
                // Process.Start(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private class ExportResult
        {
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }
            public string FilePath { get; set; }
            public int ExportedRecords { get; set; }
        }

        #endregion

    }
}
//private void ExportToExcelWithClosedXML(IEnumerable<EventForView> events, string filePath)
//{
//    using (var workbook = new XLWorkbook())
//    {
//        var worksheet = workbook.Worksheets.Add("Events");

//        // Заголовки
//        var headers = new[] { "Дата события", "Объект", "Категория события", "Описание", "Примечание", "Дата создания", "Автор" };

//        for (int i = 0; i < headers.Length; i++)
//        {
//            worksheet.Cell(1, i + 1).Value = headers[i];
//        }

//        // Данные
//        int row = 2; // Начинаем с 2-й строки (после заголовков)

//        // Используем foreach вместо for, т.к. IEnumerable не имеет Count и индексатора
//        foreach (var eventItem in events)
//        {
//            // Безопасное присвоение значений с проверкой на null
//            worksheet.Cell(row, 1).Value = eventItem.DateEvent;
//            worksheet.Cell(row, 2).Value = eventItem.Unit ?? string.Empty;
//            worksheet.Cell(row, 3).Value = eventItem.Category ?? string.Empty;
//            worksheet.Cell(row, 4).Value = eventItem.Description ?? string.Empty;
//            worksheet.Cell(row, 5).Value = eventItem.Action ?? string.Empty;
//            worksheet.Cell(row, 6).Value = eventItem.DateCreate;
//            worksheet.Cell(row, 7).Value = eventItem.Creator ?? string.Empty;

//            row++;
//        }

//        // Автоподбор ширины колонок
//        worksheet.Columns().AdjustToContents();

//        workbook.SaveAs(filePath);
//    }
//}