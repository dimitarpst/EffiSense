using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace EffiSense.Controllers
{
    public class UsageHub : Hub
    {
        public async Task SendUsageUpdate(int usageId, string applianceName, string homeName, string date, double energyUsed, int usageFrequency)
        {
            var usageData = new
            {
                UsageId = usageId,
                ApplianceName = applianceName,
                HomeName = homeName,
                Date = date,
                EnergyUsed = energyUsed,
                UsageFrequency = usageFrequency
            };

            Console.WriteLine($"📡 SignalR: Sending update for UsageId {usageId}");
            await Clients.All.SendAsync("ReceiveUsageUpdate", usageData);
        }
    }
}
