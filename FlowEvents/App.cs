using FlowEvents.Services;
using System.Windows;

namespace FlowEvents
{
    public partial class App : Application
    {


        protected override void OnExit(ExitEventArgs e)
        {
            Settings.SaveSettingsApp(); // При выходе из приложения сохраняем настройки
            base.OnExit(e);
        }

    }
}
