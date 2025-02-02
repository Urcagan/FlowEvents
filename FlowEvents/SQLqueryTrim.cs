using System;
using System.Text.RegularExpressions;

/// <summary>
/// Класс для работы со строкой SQL запроса
/// </summary>

namespace FlowEvents
{
    public static class SQLqueryTrim
    {


        /// <summary>
        /// Возвращает част SQL строки запрорса после WHERE, без ORDER BY и без фильтра LIMIT__OFFSET__
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <returns></returns>
        public static string GetWhereClauseWithoutOrderBy(string sqlQuery)
        {
            // Паттерн для поиска выражения WHERE
            string pattern = @"\bWHERE\b";

            // Получаем имя таблицы 
            string TableName = GetTableNameFromQuery(sqlQuery);
            
            // Проверяем наличие выражения WHERE в строке
            bool hasWhereClause = Regex.IsMatch(sqlQuery, pattern, RegexOptions.IgnoreCase);

            // Формируем строку COUNT(id) запроса
            string countQuery = hasWhereClause ?
                $"SELECT COUNT(id) FROM {TableName} {GetWhereClause(sqlQuery)}" :
                $"SELECT COUNT(id) FROM {TableName}";

            // В строке SQL обрезает все начиная с ORDER до конца строки
            string resultQuery = RemoveOrderByClause(countQuery);

            return resultQuery;
        }


        /// <summary>
        /// Этот метод извлекает имя таблицы из SQL запроса, следующего за ключевым словом FROM
        /// </summary>
        /// <param name="sqlQuery"> строка SQL запроса </param>
        /// <returns> Имя таблицы </returns>
        private static string GetTableNameFromQuery(string sqlQuery)
        {
            // Регулярное выражение для поиска имени таблицы после ключевого слова FROM
            Regex regex = new Regex(@"\bFROM\s+([a-zA-Z0-9_]+)\b", RegexOptions.IgnoreCase);

            // Ищем соответствие в строке SQL
            Match match = regex.Match(sqlQuery);

            string tableName = "";
            if (match.Success)
            {
                // Получаем найденое имя таблицы
                tableName = match.Groups[1].Value;
            }
            return tableName;
        }
        
        /// <summary>
        /// Возвращает из SQL строки часть строки после ключевого слова WHERE включая его
        /// </summary>
        /// <param name="sqlQuery"> Строка содержащая SQL выражение</param>
        /// <returns></returns>
        private static string GetWhereClause(string sqlQuery)
        {
            // Находим позицию начала ключевого слова WHERE
            int whereIndex = sqlQuery.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);

            if (whereIndex >= 0)
            {
                // Возвращаем подстроку, начиная с ключевого слова WHERE до конца строки
                return sqlQuery.Substring(whereIndex);
            }
            else
            {
                // Если ключевое слово WHERE не найдено, возвращаем пустую строку или обрабатываем ошибку
                return string.Empty;
            }
        }

        /// <summary>
        /// В строке SQL обрезает LIMIT __ OFFSET __
        /// </summary>
        /// <param name="sqlQuery"> Строка содержащая SQL выражение </param>
        /// <returns></returns>
        private static string RemoveLimitOffset(string sqlQuery)
        {
            // Регулярное выражение для удаления LIMIT и OFFSET
            string pattern = @"(?:\s+LIMIT\s+\d+\s+OFFSET\s+\d+\s*)?$";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

            // Удаляем LIMIT и OFFSET из запроса
            string result = regex.Replace(sqlQuery, "");

            return result.Trim(); // Обрезаем лишние пробелы в начале и конце
        }

        /// <summary>
        /// Метод удаляет строку начиная с ORDER BY до конца строки из SQL запроса 
        /// </summary>
        /// <param name="sqlQuery"></param>
        /// <returns> Строка содержащая SQL выражение </returns>
        private static string RemoveOrderByClause(string sqlQuery)
        {
            string pattern = @"\bORDER\s+BY\b.*?$";

            string result = Regex.Replace(sqlQuery, pattern, string.Empty, RegexOptions.IgnoreCase);

            return result;
        }




    }
}
