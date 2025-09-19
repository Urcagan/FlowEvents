using FlowEvents.Services;
using System.Windows;

namespace FlowEvents
{
    public partial class App : Application
    {
        private static AppSettings _settings;
        public static AppSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = AppSettings.GetSettingsApp(); // Загрузка настроек программы из файла конфигурации
                }
                return _settings;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Settings.SaveSettingsApp(); // При выходе из приложения сохраняем настройки
            base.OnExit(e);
        }

    }
}
