/*
---------------------------
 Класс для проверки базы данных
 1) CheckPathDB()   - Проверка наличия пута к базе данных
 2) checkFile()     - Проверка наличия файла базы данных по указанному пути
 3) checkTable()    - Проверка наличия таблицы хнранящей запись с версией БД
 4) checkRow()      - Проверка наличия в таблице строки с версией БД
 5) checkVre()      - Проверка версии базы данных

checkDB()           - Основной цикл условий проверки , при положительном результате взвращает true
---------------------------
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;

namespace FlowEvents
{
     
    public class CheckDB
    {        
        // ПОЛНАЯ ПРОВЕРКА ВСЕХ УСЛОВИЙ
        public static bool ALLCheckDB(DatabaseHelper databaseHelper, string PathDB, string TableName, string verProg)
        {
            if (string.IsNullOrEmpty(PathDB) || !File.Exists(PathDB)) 
            {
                MessageBox.Show(string.IsNullOrEmpty(PathDB) ? "Пожалуйста, укажите путь к базе данных 'TaskLog.db'." : "По указанному пути файла БД 'TaskLog.db' нет! \n Задайте новый путь", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
              // проверяем наличие таблицы где хранится версии БД
            else if (!databaseHelper.CheckTableName(TableName))
            {
                
                MessageBox.Show($"База данных не содержит необходимую таблицу {TableName}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            else if (!databaseHelper.CheckRow()) // проверяем наличие строки с номером версии в таблице VerDB
            {
                MessageBox.Show("В таблице VerDB отсутствуют необходимые записи о версии базы банных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (!databaseHelper.CheckVer(verProg))  //проверка версии базы данных
            {
                MessageBox.Show($"Версия БД не соответствует требуемой версии {verProg}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else    // Все условия выполнены
            {
                return true;
            }
        }

        // Проверка наличия пути и файла базы данных
        public static bool CheckPathDB(string PathDataBase) // Проверка файла БД
        {
            if (string.IsNullOrEmpty(PathDataBase) || !File.Exists(PathDataBase))
            {
                MessageBox.Show(string.IsNullOrEmpty(PathDataBase) ? "Пожалуйста, укажите путь к базе данных 'TaskLog.db'." : "По указанному пути файла БД 'TaskLog.db' нет! \n Задайте новый путь", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else    
            {
                return true;
            }
        }
    }
}
