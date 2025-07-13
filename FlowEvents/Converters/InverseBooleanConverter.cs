using System;
using System.Globalization;
using System.Windows.Data;

namespace FlowEvents
{
    //Конвертер нвертирует булевое значение при привязке данных (binding) в XAML.
    // Пример использования в XAML <Button IsEnabled = "{Binding IsBusy, Converter={StaticResource InverseBooleanConverter}}" />
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return true; // Если значение не bool (на всякий случай)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }
}
