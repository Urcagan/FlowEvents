using System;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Xml.Serialization;

namespace ShiftTaskLog
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
        public string UserName { get; set; }
        public bool IsLoggedIn { get; set; }
        
        //Путь к файлу настроек в папке AppData
        private static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "MyApp", //Папка приложения
            "settings.xml"  // Файл настроек
        );

        // Метод для загрузки настроек
        public static AppSettings Load()
        {
            try
            {
                //Если файл настроек существует, загружаем данные
                if (File.Exists(settingsPath))
                {
                    using (var fs = new FileStream(settingsPath, FileMode.Open))
                    {
                        var serializer = new XmlSerializer(typeof(AppSettings));
                        return (AppSettings)serializer.Deserialize(fs);
                    }
                }
                else
                {
                    //Если файл не найден, возвращаем новые настройки по умолчанию
                    return new AppSettings
                    {
                        UserName = "Guest",
                        IsLoggedIn = false
                    };
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке настроек: {ex.Message}","Ошибка");
                return new AppSettings(); //Вернуть настройки по умолчанию в случае ошибки
            }
        }

        //Метод для сохранения настроек
        public void Save()
        {
            try
            {
                //Убедимся что папка существует
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
                MessageBox.Show("Настройки успешно сохранены");
            }
            catch(Exception ex) 
            {
                MessageBox.Show($"Ошибка при сохранении настроек {ex.Message}");
            }
        }
    }

    
    
}
