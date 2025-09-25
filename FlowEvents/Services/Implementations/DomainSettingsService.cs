using FlowEvents.Services.Interface;

namespace FlowEvents.Services
{    

    public class DomainSettingsService : IDomainSettingsService
    {
        private readonly AppSettings _appSettings;

        // DI: получаем настройки приложения
        public DomainSettingsService(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public string GetCurrentDomainController()
        {
            return _appSettings.DomenName ?? "localhost";
        }

        public string[] GetAvailableDomainControllers()
        {
            // Логика получения доступных контроллеров домена
            return new[] { "dc1.company.com", "dc2.company.com", "localhost" };
        }

        public void SaveDomainSettings(string domainController)
        {
            _appSettings.DomenName = domainController;
            _appSettings.SaveSettingsApp();
        }
    }
}
