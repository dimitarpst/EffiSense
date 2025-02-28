using EffiSense.Controllers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EffiSense.Services
{
    public class IoTUsageSimulator : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<UsageHub> _hubContext;
        private readonly ILogger<IoTUsageSimulator> _logger;
        private static readonly Random _random = new Random();

        public IoTUsageSimulator(IServiceScopeFactory scopeFactory, IHubContext<UsageHub> hubContext, ILogger<IoTUsageSimulator> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IoT Usage Simulator Started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // ✅ Re-fetch users to ensure we have the latest interval settings
                var usersWithSimulation = await dbContext.Users
                    .Where(u => u.IsSimulationEnabled)
                    .ToListAsync();

                foreach (var user in usersWithSimulation)
                {
                    var appliances = await dbContext.Appliances
                        .Include(a => a.Home)
                        .Where(a => a.Home.UserId == user.Id)
                        .ToListAsync();

                    if (!appliances.Any()) continue;

                    var appliance = appliances[_random.Next(appliances.Count)];

                    var usage = new Usage
                    {
                        UserId = user.Id,
                        ApplianceId = appliance.ApplianceId,
                        Date = DateTime.Now,
                        Time = DateTime.Now,
                        EnergyUsed = Math.Round(_random.NextDouble() * 5, 2),
                        UsageFrequency = _random.Next(1, 6)
                    };

                    dbContext.Usages.Add(usage);
                    await dbContext.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("ReceiveUsageUpdate", new
                    {
                        UsageId = usage.UsageId,
                        ApplianceName = appliance.Name,
                        HomeName = appliance.Home.HouseName,
                        Date = usage.Date.ToString("yyyy-MM-dd"),
                        EnergyUsed = usage.EnergyUsed,
                        UsageFrequency = usage.UsageFrequency
                    });
                }

                // ✅ Dynamically fetch the interval every loop
                var minInterval = usersWithSimulation.Any()
                    ? TimeSpan.FromSeconds(usersWithSimulation.Min(u => u.SelectedSimulationInterval))
                    : TimeSpan.FromSeconds(5);

                _logger.LogInformation($"⏳ Next usage in {minInterval.TotalSeconds} seconds...");
                await Task.Delay(minInterval, stoppingToken);
            }
        }

    }
}
