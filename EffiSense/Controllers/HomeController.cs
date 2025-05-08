using EffiSense.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI_API;
using OpenAI_API.Chat;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System;                          
using System.Collections.Generic;      
using System.Linq;                     
using System.Threading.Tasks;         

namespace EffiSense.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private record ApplianceArchetype(string TypeName, string MdiIcon, string NamePromptKeywords, string NotesPromptKeywords);
        private struct TempApplianceData
        {
            public Appliance Appliance { get; init; }
            public string ArchetypeTypeName { get; init; }
        }

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
            if (user == null) return Unauthorized();

            _logger.LogInformation("FillDatabase method started for user {UserId}.", user.Id);
            var random = new Random();
            var currentUtcTime = DateTime.UtcNow;

            try
            {
                // --- Clear existing data ---
                var userHomes = _context.Homes.Where(h => h.UserId == user.Id);
                if (await userHomes.AnyAsync())
                {
                    var homeIds = await userHomes.Select(h => h.HomeId).ToListAsync();
                    var userAppliances = _context.Appliances.Where(a => homeIds.Contains(a.HomeId));
                    if (await userAppliances.AnyAsync())
                    {
                        var applianceIds = await userAppliances.Select(a => a.ApplianceId).ToListAsync();
                        var userUsages = _context.Usages.Where(u => u.UserId == user.Id && applianceIds.Contains(u.ApplianceId));
                        _context.Usages.RemoveRange(userUsages);
                    }
                    _context.Appliances.RemoveRange(userAppliances);
                    _context.Homes.RemoveRange(userHomes);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleared existing data for user {UserId}.", user.Id);
                }

                // --- Define Appliance Archetypes ---
                var applianceArchetypes = new List<ApplianceArchetype>
                {
                    new("Television", "mdi mdi-television", "Smart TVs, LED TVs, OLED displays", "about this type of television, its screen technology, smart features, or age"),
                    new("Refrigerator", "mdi mdi-fridge", "modern refrigerators, fridge-freezers", "about this refrigerator, its capacity, energy efficiency, or cooling technology"),
                    new("Washing Machine", "mdi mdi-washing-machine", "front-load or top-load washing machines", "about this washing machine, its load capacity, special cycles, or noise level"),
                    new("Vacuum Cleaner", "mdi mdi-vacuum", "robotic vacuums, upright vacuums, or handheld vacuums", "about this vacuum cleaner, its suction power, battery life, or attachments"),
                    new("Microwave Oven", "mdi mdi-microwave", "countertop microwave ovens or built-in models", "about this microwave oven, its wattage, features like grilling, or ease of use"),
                    new("Dishwasher", "mdi mdi-dishwasher", "dishwashers", "about this dishwasher, its cleaning performance, noise level, or special features"),
                    new("Light Fixture", "mdi mdi-lightbulb-on", "LED bulbs, smart bulbs, ceiling lamps, or floor lamps", "about this lighting fixture or bulb, its brightness, color temperature, or smart capabilities"),
                    new("Desktop Computer", "mdi mdi-desktop-classic", "gaming PCs, workstations, or all-in-one computers", "about this desktop computer, its performance, components, or peripherals"),
                    new("Toaster Oven", "mdi mdi-toaster-oven", "toaster ovens", "about this toaster oven, its functions like toasting, baking, or broiling"),
                    new("Fan", "mdi mdi-fan", "pedestal fans, ceiling fans, or desk fans", "about this fan, its speed settings, oscillation, or noise level"),
                    new("Kettle/Water Boiler", "mdi mdi-kettle-steam", "electric kettles or water boilers", "about this kettle, its boiling speed, capacity, or material")
                };

                // --- AI Prompts for general and specific data ---
                string homeNicknamesPrompt = "Generate 20 unique, evocative nicknames or names for homes in Bulgaria (e.g., 'Sunny Slope Villa', 'The Old Walnut Tree House', 'Eagle's Nest View'). Each on a new line.";
                string applianceBrandsPrompt = "Generate 15 unique brand names for household appliances (e.g., 'NovaTech', 'EcoPower', 'StellarHome', 'AuraAppliances'). Each on a new line.";
                string streetNamesPrompt = "Generate 30 unique and common Bulgarian street names. Include 'ул.' (street), 'бул.' (boulevard), or 'пл.' (square) prefixes. For example: 'ул. Иван Вазов', 'бул. Христо Ботев', 'пл. Свобода'. Each on a new line.";
                string homeDescriptionsPrompt = "Generate 20 unique descriptions for homes, ensuring a wide variety of lengths. Some descriptions can be null or empty, some very short (1-2 concise sentences or key phrases like 'Renovated studio.' or 'Large garden, needs work.'), some medium (3-4 sentences), and some longer (5-6 sentences). Cover aspects like style, features, and location. Each description on a new line.";


                _logger.LogInformation("Attempting to generate general data with AI...");
                var homeNicknames = await GenerateDataWithAI(homeNicknamesPrompt, 300);
                var applianceBrands = await GenerateDataWithAI(applianceBrandsPrompt, 200);
                var aiStreetNames = await GenerateDataWithAI(streetNamesPrompt, 300);
                var aiHomeDescriptions = await GenerateDataWithAI(homeDescriptionsPrompt, 2000); 

                var aiApplianceTypeSpecificNames = new Dictionary<string, List<string>>();
                var aiApplianceTypeSpecificNotes = new Dictionary<string, List<string>>();
                var aiUsageContextNotesPerType = new Dictionary<string, List<string>>();

                _logger.LogInformation("Attempting to generate archetype-specific appliance data...");
                foreach (var archetype in applianceArchetypes)
                {
                    string specificNamePrompt = $"Generate 5 unique, marketable product names for {archetype.TypeName} {archetype.NamePromptKeywords} (e.g., if type is Television, examples: 'Samsung Frame QLED 4K TV', 'Sony Bravia XR OLED', 'LG CineBeam Projector'). Each on a new line.";
                    var specificNames = await GenerateDataWithAI(specificNamePrompt, 200);
                    aiApplianceTypeSpecificNames[archetype.TypeName] = specificNames.Any() ? specificNames : new List<string> { $"Default {archetype.TypeName} Model X" };

                    string specificNotesPrompt = $"Generate 3 unique descriptive notes (each 1-4 sentences long) for a {archetype.TypeName} focusing {archetype.NotesPromptKeywords}. Ensure variety in length, some can be very short. Each note on a new line.";
                    var specificNotes = await GenerateDataWithAI(specificNotesPrompt, 400); 
                    aiApplianceTypeSpecificNotes[archetype.TypeName] = specificNotes.Any() ? specificNotes : new List<string> { $"Standard notes for {archetype.TypeName}." };

                    string specificUsageContextPrompt = $"Generate 5 unique, short context notes (1-3 sentences) explaining a plausible reason for using a {archetype.TypeName}. Examples: 'Quick cleanup with the {archetype.TypeName}.', 'Used the {archetype.TypeName} for morning routine.', 'Needed the {archetype.TypeName} for cooking dinner.'. Each on a new line.";
                    var specificUsageNotes = await GenerateDataWithAI(specificUsageContextPrompt, 300);
                    aiUsageContextNotesPerType[archetype.TypeName] = specificUsageNotes.Any() ? specificUsageNotes : new List<string> { $"Used the {archetype.TypeName}." };

                    _logger.LogInformation("Generated {CountNames} names, {CountNotes} notes, {CountUsageNotes} usage notes for archetype {Archetype}",
                        specificNames.Count, specificNotes.Count, specificUsageNotes.Count, archetype.TypeName);
                }

                homeNicknames = homeNicknames.Any() ? homeNicknames.OrderBy(x => random.Next()).ToList() : new List<string> { "Default Home Nickname" };
                applianceBrands = applianceBrands.Any() ? applianceBrands.OrderBy(x => random.Next()).ToList() : new List<string> { "Default Brand" };
                aiStreetNames = aiStreetNames.Any() ? aiStreetNames.OrderBy(x => random.Next()).ToList() : new List<string> { "ул. Стандартна" };
                aiHomeDescriptions = aiHomeDescriptions.Any() ? aiHomeDescriptions.OrderBy(x => random.Next()).ToList() : new List<string> { "A standard home description." };

                var efficiencyRatings = new List<string?> { "A+++", "A++", "A+", "A", "B", "C", "Energy Star", null };

                // --- Create Homes ---
                int homeCount = random.Next(10, 21);
                var homes = new List<Home>();
                for (int i = 0; i < homeCount; i++)
                {
                    string? description = null;
                    int descChoice = random.Next(0, 5); 
                    if (descChoice > 0 && aiHomeDescriptions.Any())
                    {
                        description = aiHomeDescriptions[i % aiHomeDescriptions.Count];
                    }

                    homes.Add(new Home
                    {
                        UserId = user.Id,
                        HouseName = homeNicknames[i % homeNicknames.Count],
                        Size = random.Next(40, 450),
                        HeatingType = random.Next(0, 3) switch { 0 => "Electric", 1 => "Gas", _ => "Central" },
                        Location = $"Bulgaria, {random.Next(1000, 9000)}",
                        Address = $"{aiStreetNames[random.Next(aiStreetNames.Count)]} {random.Next(1, 151)}",
                        BuildingType = random.Next(0, 3) switch { 0 => "Apartment", 1 => "House", _ => "Townhouse" },
                        InsulationLevel = random.Next(0, 3) switch { 0 => "Low", 1 => "Medium", _ => "High" },
                        YearBuilt = random.Next(1950, currentUtcTime.Year),
                        Description = description,
                        LastModified = currentUtcTime
                    });
                }
                await _context.Homes.AddRangeAsync(homes);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} homes.", homes.Count);

                // --- Create Appliances ---
                var tempAppliances = new List<TempApplianceData>();
                if (homes.Any())
                {
                    int applianceCountTotal = random.Next(Math.Max(homes.Count * 2, 30), homes.Count * 6);
                    for (int i = 0; i < applianceCountTotal; i++)
                    {
                        var home = homes[random.Next(homes.Count)];
                        var archetype = applianceArchetypes[random.Next(applianceArchetypes.Count)];

                        var namesForType = aiApplianceTypeSpecificNames.TryGetValue(archetype.TypeName, out var specificNamesList) && specificNamesList.Any()
                            ? specificNamesList : new List<string> { $"Default {archetype.TypeName} {i}" };

                        var notesForType = aiApplianceTypeSpecificNotes.TryGetValue(archetype.TypeName, out var specificNotesList) && specificNotesList.Any()
                            ? specificNotesList : new List<string> { $"Generic notes for {archetype.TypeName}." };

                        var appliance = new Appliance
                        {
                            HomeId = home.HomeId,
                            Name = namesForType[random.Next(namesForType.Count)],
                            Brand = applianceBrands[random.Next(applianceBrands.Count)],
                            PowerRating = $"{random.Next(50, 4000)}W",
                            EfficiencyRating = efficiencyRatings[random.Next(efficiencyRatings.Count)],
                            Notes = (random.Next(0, 2) == 0) ? null : notesForType[random.Next(notesForType.Count)],
                            PurchaseDate = (random.Next(0, 2) == 0) ? (DateTime?)null : currentUtcTime.AddDays(-random.Next(1, 365 * 10)).Date,
                            IconClass = archetype.MdiIcon,
                            LastModified = currentUtcTime
                        };
                        tempAppliances.Add(new TempApplianceData { Appliance = appliance, ArchetypeTypeName = archetype.TypeName });
                    }
                    await _context.Appliances.AddRangeAsync(tempAppliances.Select(t => t.Appliance));
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created {Count} appliances.", tempAppliances.Count);
                }

                var appliancesFromDb = await _context.Appliances
                    .Where(a => tempAppliances.Select(t => t.Appliance.ApplianceId).Contains(a.ApplianceId))
                    .ToListAsync(); 

                var applianceIdToType = tempAppliances.ToDictionary(t => t.Appliance.ApplianceId, t => t.ArchetypeTypeName);


                if (appliancesFromDb.Any())
                {
                    var usages = new List<Usage>();
                    int usageCountTotal = random.Next(appliancesFromDb.Count * 3, appliancesFromDb.Count * 10);
                    for (int i = 0; i < usageCountTotal; i++)
                    {
                        var appliance = appliancesFromDb[random.Next(appliancesFromDb.Count)];
                        string applianceTypeName = applianceIdToType.GetValueOrDefault(appliance.ApplianceId, applianceArchetypes[0].TypeName); 

                        var contextNotesForType = aiUsageContextNotesPerType.TryGetValue(applianceTypeName, out var specificContextNotesList) && specificContextNotesList.Any()
                           ? specificContextNotesList : new List<string> { $"Used the {applianceTypeName}." };

                        var randomTimeOnly = TimeOnly.FromTimeSpan(TimeSpan.FromHours(random.NextDouble() * 24));

                        usages.Add(new Usage
                        {
                            UserId = user.Id,
                            ApplianceId = appliance.ApplianceId,
                            Date = currentUtcTime.AddDays(-random.Next(1, 180)).Date,
                            Time = new DateTime(1, 1, 1, randomTimeOnly.Hour, randomTimeOnly.Minute, randomTimeOnly.Second),
                            EnergyUsed = Math.Round((decimal)(random.NextDouble() * (appliance.PowerRating.Contains("W") ? (double.Parse(Regex.Match(appliance.PowerRating, @"\d+").Value) / 1000.0) : 2.0) * random.Next(1, 5) * random.NextDouble()), 2),
                            UsageFrequency = random.Next(1, 6),
                            ContextNotes = (random.Next(0, 3) == 0) ? null : contextNotesForType[random.Next(contextNotesForType.Count)],
                            IconClass = appliance.IconClass
                        });
                    }
                    await _context.Usages.AddRangeAsync(usages);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created {Count} usages.", usages.Count);
                }

                _logger.LogInformation("Database refilled successfully for user {UserId}.", user.Id);
                return Json(new { success = true, message = "Database refilled with AI-generated detailed and coherent data! ✅" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in FillDatabase method for User: {UserId}", user?.Id ?? "Unknown");
                return StatusCode(500, new { success = false, message = "An internal server error occurred. Check server logs.", error = ex.Message });
            }
        }

        private async Task<List<string>> GenerateDataWithAI(string prompt, int maxTokens = 1000)
        {
            var generatedItems = new List<string>();
            _logger.LogInformation("Attempting AI call. Prompt: \"{PromptStart}...\", MaxTokens: {MaxTokens}", prompt.Substring(0, Math.Min(prompt.Length, 70)), maxTokens);
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("OpenAI API key is missing. Prompt: {Prompt}", prompt);
                    return new List<string> { $"Error: OpenAI API Key Missing for prompt: {prompt.Substring(0, Math.Min(prompt.Length, 30))}" };
                }

                var openAiApi = new OpenAIAPI(apiKey);
                var chatRequest = new ChatRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new[]
                    {
                        new ChatMessage(ChatMessageRole.System, "You are an assistant that generates lists of unique items. Each item should be on a new line. Do NOT include numbers, bullet points, or any other extra text unless specifically requested in the prompt. Be creative and adhere to the user's request for content and style."),
                        new ChatMessage(ChatMessageRole.User, prompt)
                    },
                    MaxTokens = maxTokens,
                    Temperature = 0.75 
                };

                var chatResult = await openAiApi.Chat.CreateChatCompletionAsync(chatRequest);
                if (chatResult?.Choices?.Count > 0 && chatResult.Choices[0].Message != null)
                {
                    string response = chatResult.Choices[0].Message.TextContent.Trim();
                    generatedItems = response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(item => item.Trim())
                                           .Where(item => !string.IsNullOrWhiteSpace(item) && item.Length > 2)
                                           .Select(item => Regex.Replace(item, @"^\s*[\d-*#]+\.?\s*", ""))
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .ToList();

                    if (!generatedItems.Any())
                    {
                        _logger.LogWarning("AI returned an empty or unparsable list for prompt: \"{PromptStart}...\" Response: {ResponseText}", prompt.Substring(0, Math.Min(prompt.Length, 70)), response.Substring(0, Math.Min(response.Length, 100)));
                        generatedItems.Add($"Default (AI empty: {prompt.Substring(0, Math.Min(prompt.Length, 30))})");
                    }
                }
                else
                {
                    _logger.LogWarning("AI returned no valid choices for prompt: \"{PromptStart}...\" Full response obj: {@ChatResult}", prompt.Substring(0, Math.Min(prompt.Length, 70)), chatResult);
                    generatedItems.Add($"Default (AI no choice: {prompt.Substring(0, Math.Min(prompt.Length, 30))})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving AI-generated data for prompt \"{PromptStart}...\"", prompt.Substring(0, Math.Min(prompt.Length, 70)));
                generatedItems.Add($"Error (AI Exception: {prompt.Substring(0, Math.Min(prompt.Length, 30))})");
            }

            if (generatedItems.Any())
            {
                _logger.LogInformation("AI successfully generated {Count} items for prompt \"{PromptStart}...\"", generatedItems.Count, prompt.Substring(0, Math.Min(prompt.Length, 30)));
            }
            else
            {
                _logger.LogWarning("AI generation resulted in zero items after processing for prompt: \"{PromptStart}...\"", prompt.Substring(0, Math.Min(prompt.Length, 30)));
            }
            return generatedItems;
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
