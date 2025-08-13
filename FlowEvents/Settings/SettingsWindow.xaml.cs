using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Windows.Shapes;
using FlowEvents.Settings;

namespace FlowEvents
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        private string _pathDB;
        private string newPathDB;
        private string _pathRelises;

        public SettingsWindow(SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            DataContext = settingsViewModel; // Устанавливаем контекст данных для привязки
            _mainViewModel = settingsViewModel._mainViewModel; 

            _pathDB = App.Settings.pathDB;
            _pathRelises = App.Settings.UpdateRepository;
        }

       

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //App.Settings.UpdateRepository = _pathRelises; //Запись пути к репозиторию обновлений

            //App.Settings.SaveSettingsApp(); 
            //this.DialogResult = true;

            //_mainViewModel.UpdateConnectionString(_pathDB);
            //_mainViewModel.LoadEvents();
            
        }
    }
}
