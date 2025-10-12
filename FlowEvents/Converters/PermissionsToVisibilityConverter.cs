using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace FlowEvents.Converters
{
    public class PermissionsToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return Visibility.Collapsed;

            var userPermissions = values[0] as List<string>;
            var requiredPermissionsStr = values[1] as string;

            if (userPermissions == null || string.IsNullOrEmpty(requiredPermissionsStr))
                return Visibility.Collapsed;

            var requiredPermissions = requiredPermissionsStr
                .Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            return userPermissions.Intersect(requiredPermissions).Any()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
