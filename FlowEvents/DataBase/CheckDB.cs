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
using System.Data.SQLite;

namespace FlowEvents
{

    public class CheckDB
    {

        //private string _connectionString = $"Data Source={Global_Var.pathDB};Version=3;";

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
                    appSettings.SaveSettingsApp(); // Сохраняем обновленные настройки

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


        public static bool CheckDatabaseFileVer(string pathDB, string VerDB)
        {

            if (!CheckDB.CheckPathToFileDB(pathDB))
            {
                return false;   // Проверяем путь к базе данных и выходим, если он неверен
            }

            string _connectionString = $"Data Source={pathDB};Version=3;foreign keys=true;"; //Формируем сторку подключения к БД

            // Проверка версии базы данных
            if (!CheckDB.IsDatabaseVersionCorrect(VerDB, _connectionString))  //проверка версии базы данных
            {
                MessageBox.Show($"Версия БД не соответствует требуемой версии {VerDB}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Проверка на соответствие версии базы данных, версии приложения 
        /// </summary>
        /// <param name="verDB"> Версия программы </param>
        /// <returns> true/false </returns>
        public static bool IsDatabaseVersionCorrect(string verDB, string connectionString)
        {
            // Формируем SQL-запрос для выбора строки, где Parameter = "Version"
            string query = $"SELECT Value FROM Config WHERE Parameter = 'VersionDB'";

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    // Получение результата запроса
                    object result = command.ExecuteScalar();

                    // Проверка, что результат не пустой
                    if (result != null)
                    {
                        string dbVersion = result.ToString();
                        // Сравнение полученного значения с переменной
                        return (dbVersion == verDB);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (System.Exception)
            {

                throw;
            }
            
        }


        // Проверка наличия пути и файла базы данных
        public static bool CheckPathToFileDB(string pathDataBase) // Проверка файла БД
        {
            if (IsPathValid(pathDataBase))
            {
                return true;
            }  

            ShowErrorMessage(pathDataBase);
            return false;
        }

        // Проверка, является ли путь корректным и существует ли файл
        private static bool IsPathValid(string pathDataBase)
        {
            return !string.IsNullOrEmpty(pathDataBase) && File.Exists(pathDataBase);
        }

        // Отображение сообщения об ошибке в зависимости от состояния пути
        private static void ShowErrorMessage(string pathDataBase)
        {
            string errorMessage = string.IsNullOrEmpty(pathDataBase)
                        ? "Пожалуйста, укажите путь к базе данных 'TaskLog.db'."
                        : "По указанному пути файла БД 'TaskLog.db' нет! \n Задайте новый путь";

            MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }


    }

}
