using System;
using System.Globalization;
using System.Security.Principal;

namespace ShiftTaskLog
{
    public static class Global_Var
    {
        public const Int32 BUFFER_SIZE = 512; // Unmodifiable
        public static String FILE_NAME = "Output.txt"; // Modifiable
        public static readonly String CODE_PREFIX = "US-"; // Unmodifiable

        //public static string pathDB = Properties.Settings.Default.selectedPathDB;   // Получаем из свойств путь к базе данных
        //public static string pathDB = "D:\\VS_Dev\\EventFlow.db";
        public static string pathDB = "G:\\VS Dev\\FlowEvents\\FlowEvents.db";

        public static WindowsIdentity UserId = WindowsIdentity.GetCurrent();   // статическая переменная UserId типа WindowsIdentity, которая получает текущую идентичность пользователя Windows
        public static string User_Name_Full = UserId.Name;  //Это свойство Name возвращает имя пользователя в формате, который обычно используется в Windows для идентификации пользователей.
        public static string User_Name = UserId.Name.Substring(UserId.Name.IndexOf("\\") + 1); // Получаем только имя пользователя без домена
        public static string User_Role;

        
        public static DateTime StartDate; // Начальная дата вывода для диапазона 
        public static DateTime EndDate;   // Конечная дата вывода для диапазона 
        public static DateTime OneDate;   // Переменая для хранения только одной даты
        public static State StateDate;  // Выбор режима используемой даты (один день - 0 / диапазон дат - 1)

        public static bool  TimerUpdaterRun = false; // Прерсенная состояния выполнения задачи по таймеру

        //Режимы выбора даты
        public enum State
        {
            All,
            OneDate,
            RangeDate
        }
    }

    /**
    //Класс для хранения конфигурации приложения
    public static class AppConfig
    {
        public static readonly CultureInfo culture = new CultureInfo("ru-RU"); // Культура русского языка
        public static readonly string formatDate = "yyyy-MM-dd"; //Формат даты в приложении
        public static readonly string formatDateTime = "yyyy-MM-dd HH:mm:ss"; // формат даты с временем

        public static int TimeAutoUpdate = Properties.Settings.Default.TimeAutoUpdate;  // интервал времени обновления 
        public static bool AutoUpdate = Properties.Settings.Default.AutoUpdate;     // Вкл/Выкд автоматическкого обновления
        public static int pageSize = Properties.Settings.Default.pageSize;   // Количество строк на листе
        public static string RowOrder; // Порядок отображения строк
        public static bool Notifier = Properties.Settings.Default.Notifier; // Всплывающее сообщение 
        public static int NotifierTime = Properties.Settings.Default.NotifierTime;  //Длительность всплывающнго сообщения минут.

        public static void AppConfigSave()
        {
            Properties.Settings.Default.TimeAutoUpdate = TimeAutoUpdate;
            Properties.Settings.Default.AutoUpdate = AutoUpdate;
            Properties.Settings.Default.pageSize = pageSize;
            Properties.Settings.Default.RowOrder = RowOrder;
            Properties.Settings.Default.Notifier = Notifier;
            Properties.Settings.Default.NotifierTime = NotifierTime;

            Properties.Settings.Default.Save();
        }


        /// <summary>
        /// Сохранение выбранных дат 
        /// </summary>
        public static void SaveSelectedDate()
        {
            Properties.Settings.Default.StateDate = Enum.GetName(typeof(Global_Var.State), Global_Var.StateDate);
            Properties.Settings.Default.OneDate = Global_Var.OneDate;
            Properties.Settings.Default.StartDate = Global_Var.StartDate;
            Properties.Settings.Default.EndDate = Global_Var.EndDate;

            Properties.Settings.Default.Save();
        } 
    }
    **/
    

}
