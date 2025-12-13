using System.Threading.Tasks;

namespace TestAspNetCore.Hubs.Interfaces
{
    public interface IChatClient
    {
        Task ReceiveMessage(string user, string message);
        Task ReceiveSystemNotification(string notification);
    }
}
