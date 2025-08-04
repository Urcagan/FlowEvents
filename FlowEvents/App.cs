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
                    _settings = AppSettings.GetSettingsApp();
                }
                return _settings;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // При выходе из приложения сохраняем настройки
            Settings.SaveSettingsApp();
            base.OnExit(e);
        }
    }
}
