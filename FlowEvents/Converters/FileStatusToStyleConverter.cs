using FlowEvents.Models.Enums;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FlowEvents
{
    public class FileStatusToStyleConverter : IValueConverter
    {
        public Style NormalStyle { get; set; }
        public Style DeletedStyle { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileStatus status && status == FileStatus.Deleted)
                return DeletedStyle;

            return NormalStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is FileStatus status && status != FileStatus.Deleted)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

   

}
