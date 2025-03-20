using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Xml.Serialization;

namespace FlowEvents
{

    public static class AppBaseConfig
    {
        public static WindowsIdentity UserId = WindowsIdentity.GetCurrent();   // статическая переменная UserId типа WindowsIdentity, которая получает текущую идентичность пользователя Windows
        public static string User_Name_Full = UserId.Name;  //Это свойство Name возвращает имя пользователя в формате, который обычно используется в Windows для идентификации пользователей.
        public static string User_Name = UserId.Name.Substring(UserId.Name.IndexOf("\\") + 1); // Получаем только имя пользователя без домена
        public static string User_Role;

        public static readonly CultureInfo culture = new CultureInfo("ru-RU"); // Культура русского языка
        public static readonly string formatDate = "yyyy-MM-dd"; //Формат даты в приложении
        public static readonly string formatDateTime = "yyyy-MM-dd HH:mm:ss"; // формат даты с временем
    }

    public class AppSettings
    {
        // Параметры програы с параметрами по умолчанию
        public string pathDB { get; set; } = "C:\\";  // Путь к базе данных по умолчанию
        public string VerDB { get; set; } = "0.1.0"; //Версия БД для проверки

        //Путь к файлу настроек в папке AppData
        // private static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        //     "MyApp", //Папка приложения
        //     "settings.xml"  // Файл настроек
        // );
        // Этот код вернет путь к каталогу, где находится ваш исполняемый файл.exe вашего приложения.
        // Assembly.GetExecutingAssembly().Location — возвращает путь к исполняемому файлу.
        // Path.GetDirectoryName(...) — извлекает директорию из полного пути.

        private static string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.xml"); // Файл настроек
        
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
                // Загружаем существующий файл настроек
                using (var fs = new FileStream(settingsPath, FileMode.Open))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    return (AppSettings)serializer.Deserialize(fs);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new AppSettings(); //Вернуть настройки по умолчанию в случае ошибки
            }
        }

        //Метод для сохранения настроек
        public void SaveSettingsApp()
        {
            try
            {
                // Убедимся, что папка для настроек существует
                var directory = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                //Сериализация настроек в XML
                using (var fs = new FileStream(settingsPath, FileMode.Create))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    serializer.Serialize(fs, this);
                }
               // MessageBox.Show("Настройки успешно сохранены");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
