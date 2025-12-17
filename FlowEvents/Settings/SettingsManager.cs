using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FlowEvents
{
    public static class AppBaseConfig
    {
        //public static WindowsIdentity UserId = WindowsIdentity.GetCurrent();   // статическая переменная UserId типа WindowsIdentity, которая получает текущую идентичность пользователя Windows
        //public static string User_Name_Full = UserId.Name;  //Это свойство Name возвращает имя пользователя в формате, который обычно используется в Windows для идентификации пользователей.
        //public static string User_Name = UserId.Name.Substring(UserId.Name.IndexOf("\\") + 1); // Получаем только имя пользователя без домена
        //public static string User_Role;

        public static readonly CultureInfo culture = new CultureInfo("ru-RU"); // Культура русского языка
        public static readonly string formatDate = "yyyy-MM-dd"; //Формат даты в приложении
        public static readonly string formatDateTime = "yyyy-MM-dd HH:mm:ss"; // формат даты с временем
    }

    public class AppSettings
    {
        // Свойства для окна
        public double WindowLeft { get; set; } = -1; // -1 означает "не установлено"
        public double WindowTop { get; set; } = -1;
        public double WindowWidth { get; set; } = -1;
        public double WindowHeight { get; set; } = -1;
        public string WindowState { get; set; } = "Normal";

        // Параметры програы с параметрами по умолчанию
        public string pathDB { get; set; } = "C:\\";  // Путь к базе данных по умолчанию
        public int VerDB { get; set; } = 1; //Версия БД для проверки
        public string UpdateRepository { get; set; } =  "C:\\Releases"; //Путь к парке с обновлениями

        public string DomenName { get; set; } = "localhost"; // Имя домена по умолчанию

        public int DefaultRole { get; set; } = 1; // Роль присваеваемая пользователю по умолчанию 

        // Свойства для хранения ширины столбцов
        // Ключ - заголовок столбца, значение - ширина в пикселях
        public Dictionary<string, double> DataGridColumnWidths { get; set; } = new Dictionary<string, double>
        {
            {"Дата", 100},
            {"Установка", 150},
            {"Переработка", 150},
            {"Вид", 100},
            {"Событие", 200},
            {"Примечание, пояснение", 200},
            {"Документ", 200},
            {"Мониторинг + Диспетчерский отчет", 150},
            {"Автор", 100}
        };

        // Свойства для автообновления
        public bool AutoRefreshEnabled { get; set; } = false;
        public int AutoRefreshInterval { get; set; } = 300; // секунды


        //Свойства для обеспечения экспорта отчета в Excel
        public bool AutoOpenExcel { get; set; } = false; // Автоматическое открытие Excel после завершения экспорта.


        //Путь к файлу настроек в папке AppData
        // private static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        //     "MyApp", //Папка приложения
        //     "settings.xml"  // Файл настроек
        // );
        // Этот код вернет путь к каталогу, где находится ваш исполняемый файл.exe вашего приложения.
        // Assembly.GetExecutingAssembly().Location — возвращает путь к исполняемому файлу.
        // Path.GetDirectoryName(...) — извлекает директорию из полного пути.

        private static string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json"); // Файл настроек

        public float SizeFileAttachment { get; set; } = 10; // Максимальный размер файла вложения в МБ

        // Метод для загрузки настроек
        public static AppSettings GetSettingsApp()
        {
            try
            {
                // Проверяем, существует ли файл конфигурации
                if (!File.Exists(settingsPath))
                {
                    // Если нет, создаем новый файл настроек с параметрами по умолчанию
                    var defaultSettings = new AppSettings();
                    defaultSettings.SaveSettingsApp();  // Сохранение нового файла
                    return defaultSettings;
                }
                var json = File.ReadAllText(settingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке настроек: {ex.Message}", 
                                "Ошибка", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
                return new AppSettings(); //Вернуть настройки по умолчанию в случае ошибки
            }
        }

        //Метод для сохранения настроек с проверкой наличия изменеия методом сравнения хеша настроек текущих и в файле
        public void SaveSettingsApp()
        {
            try
            {
                var currentJson = JsonConvert.SerializeObject(this, Formatting.Indented); // серриализуем текущие настройки
                var currentHash = ComputeHash(currentJson); //Считаем хеш 

                if (File.Exists(settingsPath)) //Проверяем наличие файла
                {
                    var lastSavedJson = File.ReadAllText(settingsPath);
                    var lastSavedHash = ComputeHash(lastSavedJson);

                    if (currentHash == lastSavedHash)
                    {
                        return;
                    }
                }
                // Создаем директорию, если ее нет
                var directory = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                //var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                //File.WriteAllText(settingsPath, json);

                // Сохраняем только если есть изменения
                File.WriteAllText(settingsPath, currentJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", 
                                "Ошибка", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
            }
        }

        // Расчет хеша
        private string ComputeHash(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Метод для инициализации ширины столбцов по умолчанию из DataGrid
        public void InitializeDefaultColumnWidths(DataGrid dataGrid)
        {
            DataGridColumnWidths = new Dictionary<string, double>();

            foreach (var column in dataGrid.Columns)
            {
                var header = column.Header?.ToString();
                if (!string.IsNullOrEmpty(header))
                {
                    // Используем ActualWidth если она есть, иначе - Width
                    double width = column.ActualWidth > 0 ? column.ActualWidth :
                                  (column.Width.IsAbsolute ? column.Width.Value : 150);

                    DataGridColumnWidths[header] = width;
                }
            }
        }
    }

}
