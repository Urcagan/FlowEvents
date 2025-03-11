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
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace FlowEvents
{

    public class CheckDB
    {

        // Проверка наличия файла БД
        public static bool CheckDatabaseFile(AppSettings appSettings)
        {
            // Проверяем, указан ли путь и существует ли файл
            if (string.IsNullOrWhiteSpace(appSettings.pathDB) || !File.Exists(appSettings.pathDB))
            {
                MessageBox.Show("Файл базы данных не найден. Укажите корректный путь.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Открываем диалог выбора файла
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Выберите файл базы данных",
                    Filter = "Файлы базы данных (*.db;*.sqlite)|*.db;*.sqlite|Все файлы (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Обновляем путь в настройках
                    appSettings.pathDB = openFileDialog.FileName;
                    appSettings.Save(); // Сохраняем обновленные настройки

                    MessageBox.Show("Новый путь к базе данных сохранен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true; // Файл выбран и путь обновлен
                }
                else
                {
                    MessageBox.Show("Программа не может продолжить без базы данных!", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                    return false; // Файл не выбран
                    // Application.Current.Shutdown(); // Закрываем приложение
                }
            }
            return true; // Файл существует
        }

        // ПОЛНАЯ ПРОВЕРКА ВСЕХ УСЛОВИЙ
        public static bool ALLCheckDB(DatabaseHelper databaseHelper, string PathDB, string tableName, string verProg)
        {
            if (string.IsNullOrEmpty(PathDB) || !File.Exists(PathDB))
            {
                MessageBox.Show(string.IsNullOrEmpty(PathDB) ? "Пожалуйста, укажите путь к базе данных 'TaskLog.db'." : "По указанному пути файла БД 'TaskLog.db' нет! \n Задайте новый путь", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            // проверяем наличие таблицы где хранится версии БД
            else if (!databaseHelper.CheckTableName(tableName))
            {

                MessageBox.Show($"База данных не содержит необходимую таблицу {tableName}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            else if (!databaseHelper.CheckRow(tableName)) // проверяем наличие строки с номером версии в таблице VerDB
            {
                MessageBox.Show($"В таблице {tableName} отсутствуют необходимые записи о версии базы банных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (!databaseHelper.CheckVer(tableName, verProg))  //проверка версии базы данных
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
