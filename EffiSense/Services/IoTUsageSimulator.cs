using EffiSense.Controllers;
using EffiSense.Data;
using EffiSense.Models;
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

        private static readonly string[] _contextExamples = new[] {
            "Regular evening use", "Quick morning check", "Left on accidentally",
            "Weekend binge watching", "Holiday cooking prep", "Running while away",
            "Testing new settings", "Background process", null, null, null, null, null
        };

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
                int calculatedIntervalSeconds = 5;
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var usersWithSimulation = await dbContext.Users
                        .Where(u => u.IsSimulationEnabled)
                        .Select(u => new { u.Id, u.SelectedSimulationInterval })
                        .ToListAsync(stoppingToken);

                    if (usersWithSimulation.Any())
                    {
                        foreach (var userSimInfo in usersWithSimulation)
                        {
                            try
                            {
                                var appliances = await dbContext.Appliances
                                    .Include(a => a.Home)
                                    .Where(a => a.Home.UserId == userSimInfo.Id)
                                    .ToListAsync(stoppingToken);

                                if (!appliances.Any()) continue;

                                var appliance = appliances[_random.Next(appliances.Count)];

                                DateTime now = DateTime.Now;
                                DateTime usageTimestamp = now.Date.Add(now.TimeOfDay);

                                var usage = new Usage
                                {
                                    UserId = userSimInfo.Id,
                                    ApplianceId = appliance.ApplianceId,
                                    Date = usageTimestamp,
                                    Time = now, // Keep if needed, Date now holds combined
                                    EnergyUsed = (decimal)Math.Round(_random.NextDouble() * 5, 2),
                                    UsageFrequency = _random.Next(1, 6),
                                    ContextNotes = _contextExamples[_random.Next(_contextExamples.Length)], // Assign random note
                                    IconClass = appliance.IconClass // Assign icon from appliance
                                };

                                dbContext.Usages.Add(usage);
                                await dbContext.SaveChangesAsync(stoppingToken);

                                await _hubContext.Clients.All.SendAsync("ReceiveUsageUpdate", new
                                {
                                    UsageId = usage.UsageId,
                                    ApplianceName = appliance.Name,
                                    HomeName = appliance.Home?.HouseName ?? "N/A",
                                    Date = usage.Date.ToString("yyyy-MM-dd HH:mm"),
                                    EnergyUsed = usage.EnergyUsed,
                                    UsageFrequency = usage.UsageFrequency,
                                    ContextNotes = usage.ContextNotes, // Send new field
                                    IconClass = usage.IconClass // Send new field
                                }, stoppingToken);

                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing simulation for user {UserId}", userSimInfo.Id);
                            }
                        }

                        calculatedIntervalSeconds = usersWithSimulation.Min(u => u.SelectedSimulationInterval);
                        if (calculatedIntervalSeconds < 1) calculatedIntervalSeconds = 1;
                    }
                    else
                    {
                        calculatedIntervalSeconds = 5;
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the IoT Usage Simulator loop.");
                    calculatedIntervalSeconds = 60;
                }

                try
                {
                    TimeSpan delay = TimeSpan.FromSeconds(calculatedIntervalSeconds);
                    _logger.LogInformation("IoT Usage Simulator: Next check in {DelaySeconds} seconds.", delay.TotalSeconds);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
            }

            _logger.LogInformation("IoT Usage Simulator Stopped.");
        }
    }
}
