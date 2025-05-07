using EffiSense.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI_API.Chat;
using OpenAI_API;
using System.Diagnostics;
using System.Configuration;

namespace EffiSense.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var categoryData = _context.Usages
                .Include(u => u.Appliance)
                .Where(u => u.UserId == user.Id)
                .GroupBy(u => u.Appliance.Name)
                .Select(g => new
                {
                    Appliance = g.Key,
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .ToList();

            var labels = categoryData.Select(d => d.Appliance).ToArray();
            var energyUsed = categoryData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { labels, energyUsed });
        }

        [HttpGet]
        public async Task<IActionResult> GetDayOfWeekData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var usages = await _context.Usages
                .Where(u => u.UserId == user.Id)
                .ToListAsync(); // ✅ Force in-memory evaluation

            var dayOfWeekData = usages
                .GroupBy(u => u.Date.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key.ToString(),
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .ToList();

            var daysOfWeek = dayOfWeekData.Select(d => d.DayOfWeek).ToArray();
            var energyUsed = dayOfWeekData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { daysOfWeek, energyUsed });

        }



        [HttpGet]
        public async Task<IActionResult> GetMonthlyUsageData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var usages = await _context.Usages
                .Where(u => u.UserId == user.Id)
                .ToListAsync(); // ✅ Forces in-memory processing

            var monthlyUsageData = usages
                .GroupBy(u => new { u.Date.Year, u.Date.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .OrderBy(d => d.Month)
                .ToList();

            var months = monthlyUsageData.Select(d => d.Month).ToArray();
            var energyUsed = monthlyUsageData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { months, energyUsed });
        }


        [HttpGet]
        public async Task<IActionResult> GetBuildingTypeData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var buildingData = _context.Usages
                .Include(u => u.Appliance)
                .ThenInclude(a => a.Home)
                .Where(u => u.UserId == user.Id)
                .GroupBy(u => u.Appliance.Home.BuildingType)
                .Select(g => new
                {
                    BuildingType = g.Key,
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .ToList();

            var buildingTypes = buildingData.Select(d => d.BuildingType).ToArray();
            var energyUsed = buildingData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { buildingTypes, energyUsed });
        }


        [HttpGet]
        public async Task<IActionResult> GetApplianceData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var userId = user.Id;

            var applianceData = _context.Usages
                .Where(u => u.UserId == userId)
                .GroupBy(u => u.Appliance.Name)
                .Select(g => new
                {
                    ApplianceName = g.Key,
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .ToList();

            var applianceNames = applianceData.Select(d => d.ApplianceName).ToArray();
            var energyUsed = applianceData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { applianceNames, energyUsed });
        }

        [HttpGet]
        public async Task<IActionResult> GetHomeData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var homeData = _context.Usages
                .Include(u => u.Appliance)
                .ThenInclude(a => a.Home)
                .Where(u => u.UserId == user.Id)
                .GroupBy(u => u.Appliance.Home.HouseName)
                .Select(g => new
                {
                    HomeName = g.Key,
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .ToList();

            var homeNames = homeData.Select(d => d.HomeName).ToArray();
            var energyUsed = homeData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { homeNames, energyUsed });
        }

        [HttpGet]
        public async Task<IActionResult> GetPeakTimeData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var peakTimeData = _context.Usages
                .Where(u => u.UserId == user.Id)
                .GroupBy(u => u.Time.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .OrderBy(g => g.Hour)
                .ToList();

            var hours = peakTimeData.Select(d => $"{d.Hour}:00").ToArray();
            var energyUsed = peakTimeData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { hours, energyUsed });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsageData()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); 
            }

            var userId = user.Id;

            var usageData = _context.Usages
                .Where(u => u.UserId == userId)
                .GroupBy(u => u.Date.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    EnergyUsed = g.Sum(u => u.EnergyUsed)
                })
                .ToList();

            var labels = usageData.Select(d => d.Date).ToArray();
            var energyUsed = usageData.Select(d => d.EnergyUsed).ToArray();

            return Json(new { labels, energyUsed });
        }

        [HttpPost]
        public async Task<IActionResult> FillDatabase()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var random = new Random();

            var userHomes = _context.Homes.Where(h => h.UserId == user.Id);
            if (userHomes.Any())
            {
                var homeIds = userHomes.Select(h => h.HomeId);
                var userAppliances = _context.Appliances.Where(a => homeIds.Contains(a.HomeId));
                var applianceIds = userAppliances.Select(a => a.ApplianceId);
                var userUsages = _context.Usages.Where(u => applianceIds.Contains(u.ApplianceId));

                _context.Usages.RemoveRange(userUsages);
                _context.Appliances.RemoveRange(userAppliances);
                _context.Homes.RemoveRange(userHomes);
                await _context.SaveChangesAsync();
            }

            var homeNames = await GenerateDataWithAI("Generate 20 unique Bulgarian-themed home names.");
            var applianceNames = await GenerateDataWithAI("Generate 50 unique names for household appliances.");
            var applianceBrands = await GenerateDataWithAI("Generate 10 unique brand names for household appliances.");


            homeNames = homeNames.OrderBy(x => random.Next()).ToList();
            applianceNames = applianceNames.OrderBy(x => random.Next()).ToList();
            applianceBrands = applianceBrands.OrderBy(x => random.Next()).ToList();

            int homeCount = random.Next(10, 21);
            var homes = new List<Home>();

            for (int i = 0; i < homeCount; i++)
            {
                homes.Add(new Home
                {
                    UserId = user.Id,
                    HouseName = homeNames[i],
                    Size = random.Next(50, 300),
                    HeatingType = random.Next(0, 2) == 0 ? "Electric" : "Gas",
                    Location = $"Bulgaria, {random.Next(1000, 9000)}",
                    Address = $"Ul. {random.Next(1, 100)}, {homeNames[i]}",
                    BuildingType = random.Next(0, 2) == 0 ? "Apartment" : "House",
                    InsulationLevel = random.Next(0, 2) == 0 ? "Low" : "High"
                });
            }

            await _context.Homes.AddRangeAsync(homes);
            await _context.SaveChangesAsync();

            int applianceCount = random.Next(50, 71);
            var appliances = new List<Appliance>();

            for (int i = 0; i < applianceCount; i++)
            {
                var home = homes[random.Next(homes.Count)];
                appliances.Add(new Appliance
                {
                    HomeId = home.HomeId,
                    Name = applianceNames[i % applianceNames.Count],
                    Brand = applianceBrands[random.Next(applianceBrands.Count)],
                    PowerRating = $"{random.Next(500, 2000)}W"
                });
            }

            await _context.Appliances.AddRangeAsync(appliances);
            await _context.SaveChangesAsync();

            int usageCount = random.Next(100, 201);
            var usages = new List<Usage>();

            for (int i = 0; i < usageCount; i++)
            {
                var appliance = appliances[random.Next(appliances.Count)];
                usages.Add(new Usage
                {
                    UserId = user.Id,
                    ApplianceId = appliance.ApplianceId,
                    Date = DateTime.Now.AddDays(-random.Next(1, 30)),
                    Time = DateTime.Now.AddHours(-random.Next(1, 24)),
                    EnergyUsed = (decimal)Math.Round(random.NextDouble() * 5, 2),
                    UsageFrequency = random.Next(1, 6)
                });
            }

            await _context.Usages.AddRangeAsync(usages);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Database refilled with AI-generated names! ✅" });
        }


        private async Task<List<string>> GenerateDataWithAI(string prompt)
        {
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];
                var openAiApi = new OpenAIAPI(apiKey);

                var chatRequest = new ChatRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new[]
                    {
                new ChatMessage(ChatMessageRole.System, "You are an assistant that generates lists of unique names. Return ONLY the list of names, each on a new line. DO NOT include numbers, bullet points, or any extra text."),
                new ChatMessage(ChatMessageRole.User, prompt)
            },
                    MaxTokens = 200
                };

                var chatResult = await openAiApi.Chat.CreateChatCompletionAsync(chatRequest);
                string response = chatResult.Choices[0].Message.TextContent.Trim();

                var nameList = response.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(name => name.Trim())
                                       .Where(name => !string.IsNullOrWhiteSpace(name))
                                       .ToList();

                nameList = nameList.Select(name => System.Text.RegularExpressions.Regex.Replace(name, @"^\d+\.\s*", "")).ToList();

                return nameList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving AI-generated data: {ex.Message}");
                return new List<string> { "ErrorGeneratingData" };
            }
        }


        [HttpPost]
        public async Task<IActionResult> ToggleSimulation(bool enable, int interval)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            using var scope = _context;

            user.IsSimulationEnabled = enable;
            user.SelectedSimulationInterval = interval;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = enable ? "Simulation started." : "Simulation stopped." });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateSimulationInterval(int interval)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            user.SelectedSimulationInterval = interval;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Interval updated to {interval}s." });
        }


        [HttpGet]
        public async Task<IActionResult> GetSimulationState()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            return Json(new
            {
                success = true,
                isRunning = user.IsSimulationEnabled,
                interval = user.SelectedSimulationInterval
            });
        }


    }
}
