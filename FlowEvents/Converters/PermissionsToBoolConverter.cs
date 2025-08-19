using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FlowEvents
{
    [ValueConversion(typeof(List<string>), typeof(bool))]
    public class PermissionsToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] - список прав пользователя (CurrentUserPermissions)
            // values[1] - строка с требуемыми правами (из ConverterParameter)

            if (values.Length < 2) return false;
            if (!(values[0] is List<string> userPermissions)) return false;
            if (!(values[1] is string requiredPermissionsStr)) return false;

            // Разделяем требуемые права (формат: "EditDocument,ViewReports")
            var requiredPermissions = requiredPermissionsStr
                .Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            // Проверяем наличие хотя бы одного из требуемых прав
            return userPermissions.Intersect(requiredPermissions).Any();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
