namespace TestAspNetCore.Services
{
    public interface IChatService
    {
        void ProcessMessage(string user, string msg);
    }

    public class ChatService : IChatService
    {
        public void ProcessMessage(string user, string msg)
        {
            // Simple logic: just print to console for now, or log
            System.Console.WriteLine($"[ChatService] Processing: {user} -> {msg}");
        }
    }
}
