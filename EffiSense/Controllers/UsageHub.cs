using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace EffiSense.Controllers
{
    public class UsageHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"🔌 Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"🔌 Client disconnected: {Context.ConnectionId}");
            if (exception != null)
            {
                Console.WriteLine($"   Reason: {exception.Message}");
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
