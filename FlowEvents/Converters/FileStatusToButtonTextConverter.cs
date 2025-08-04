using FlowEvents.Models.Enums;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace FlowEvents
{
    public class FileStatusToButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"Конвертер вызван. Текущий статус: {value}");
            return (value is FileStatus status && status == FileStatus.Deleted)
                ? "Отменить удаление"
                : "Удалить";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
