using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    internal class MainViewModel : INotifyPropertyChanged
    {

        public MainViewModel() 
        {
            PathLoad();
        }

        private void PathLoad()
        {
            // Загружаем настройки при запуске программы
            appSettings = AppSettings.Load();

            //Необходимо проверить есть ли файл базы данных по указанному пути

            // Проверяем, существует ли база данных
            if (!CheckDB.CheckDatabaseFile(appSettings))
            {
                //Application.Current.Shutdown(); // Закрываем приложение, если файл не выбран
                MessageBox.Show("Необходимо выбрать базу данных !!!");
            }
            else
            {
                Global_Var.pathDB = appSettings.pathDB;

                databaseHelper = new DatabaseHelper(Global_Var.pathDB);    // Инициализация копии класса работы с БД

                //НЕОБХОДИМО ПЕРЕДЕЛАТЬ ДЛЯ ВЫВОДА ПУТИ В ОКНО
                //lblPath.Text = "Путь: " + Global_Var.pathDB; //Global_Var.pathDB;

                
                // Проверка базы данных на исправность
                if (CheckDB.ALLCheckDB(databaseHelper, Global_Var.pathDB, "Config", appSettings.VerDB))
                {
                    // Создаем и показываем окно настроек
                    //SettingsWindow settingsWindow = new SettingsWindow( databaseHelper );
                    //settingsWindow.ShowDialog(); // Открываем окно как модальное
                    IsCheckDB = true;
                }

                //Execute();
            }
        }


        //===============================================================================================================================================
        //Поля, Переменный, Свойства

        private AppSettings appSettings; // Объект параметров приложения
        public DatabaseHelper databaseHelper;
        private bool IsCheckDB = false;





        // Реализация INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для генерации события
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
