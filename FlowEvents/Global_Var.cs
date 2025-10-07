using System;
using System.Data.Common;
using System.Globalization;
using System.Security.Principal;

namespace FlowEvents
{
    public static class Global_Var
    {
        public static string UserName;
        
        public const Int32 BUFFER_SIZE = 512; // Unmodifiable
        public static String FILE_NAME = "Output.txt"; // Modifiable
        public static readonly String CODE_PREFIX = "US-"; // Unmodifiable

        

        public static WindowsIdentity UserId = WindowsIdentity.GetCurrent();   // статическая переменная UserId типа WindowsIdentity, которая получает текущую идентичность пользователя Windows
        public static string User_Name_Full = UserId.Name;  //Это свойство Name возвращает имя пользователя в формате, который обычно используется в Windows для идентификации пользователей.
        public static string User_Name = UserId.Name.Substring(UserId.Name.IndexOf("\\") + 1); // Получаем только имя пользователя без домена
        public static string User_Role;



        public static bool  TimerUpdaterRun = false; // Прерсенная состояния выполнения задачи по таймеру

        //Режимы выбора даты
        public enum State
        {
            All,
            OneDate,
            RangeDate
        }
    }

}
