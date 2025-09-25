namespace FlowEvents.Services.Interface
{
    public interface IDomainSettingsService
    {
        string GetCurrentDomainController();
        string[] GetAvailableDomainControllers();
        void SaveDomainSettings(string domainController);
    }
}
