using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Коллекция для хранения данных (автоматически уведомляет об изменениях)
        public ObservableCollection<EventsModel> Events { get; set; } = new ObservableCollection<EventsModel>();

        // Сервис для работы с базой данных
        private readonly IDatabaseService _databaseService;

        public MainViewModel( IDatabaseService databaseService) 
        {
            _databaseService = databaseService;

            // Загрузка данных при создании ViewModel
            LoadEvents();

            //PathLoad();
        }


        // Метод для загрузки данных из базы
        private void LoadEvents()
        {
            // Очищаем коллекцию перед загрузкой новых данных
            Events.Clear();

            // Получаем данные из базы
            var eventsFromDb = _databaseService.GetEvents();

            // Добавляем данные в коллекцию
            foreach (var eventModel in eventsFromDb)
            {
                Events.Add(eventModel);
            }
        }

        private void PathLoad()
        {

            appSettings = AppSettings.Load(); // Загружаем настройки программы из файла при запуске программы
            string pathDB = appSettings.pathDB;
            
            if ( !CheckDB.CheckPathDB(pathDB)) 
                return;  // Проверяем путь к базе данных и выходим, если он неверен

            Global_Var.pathDB = pathDB; //Записываем в глобальную переменную

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
