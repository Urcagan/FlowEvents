
namespace FlowEvents.Services.Interface
{
    public interface IConnectionStringProvider
    {
        string GetConnectionString();
        void UpdateConnectionString(string newConnectionString);
    }
}
