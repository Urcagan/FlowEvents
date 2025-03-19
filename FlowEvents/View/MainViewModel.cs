using FlowEvents.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace FlowEvents
{
    public class MainViewModel : INotifyPropertyChanged
    {
       
        // Коллекция для хранения данных (автоматически уведомляет об изменениях)
        public ObservableCollection<EventsModel> Events { get; set; } = new ObservableCollection<EventsModel>();

        // Сервис для работы с базой данных
        private IDatabaseService _databaseService;
        private readonly AppSettings _appSettings;

        public MainViewModel( IDatabaseService databaseService, AppSettings appSettings) 
        {
            _databaseService = databaseService;
            _appSettings = appSettings;


            // Проверка наличия файла базы данных перед загрузкой данных
            if (CheckDatabaseFile())
            {
                LoadEvents();   // Загрузка данных при создании ViewModel
            }

            //PathLoad();
        }

        // Метод для обновления пути к базе данных
    public void UpdateDatabasePath(string newPath)
        {
            _appSettings.pathDB = newPath;
            _appSettings.Save();

            // Пересоздаем сервис базы данных с новым путем
            _databaseService = new DatabaseService(newPath);

            // Перезагружаем данные
            LoadEvents();
        }

        private bool CheckDatabaseFile()
        {
            if (!File.Exists(_appSettings.pathDB))
            {
                MessageBox.Show("Файл базы данных не найден. Пожалуйста, укажите новый путь к базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
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
