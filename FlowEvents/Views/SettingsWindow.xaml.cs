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
        public SettingsWindow()
        {
            InitializeComponent();
         
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
