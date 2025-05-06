using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using EffiSense.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenAI_API;
using OpenAI_API.Chat;


namespace EffiSense.Controllers
{
    [Authorize]
    public class UsagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public UsagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        private async Task<string> GetEnergyEfficiencyTips(string prompt)
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
                new ChatMessage(ChatMessageRole.System, "You are an assistant that provides energy-efficiency tips. Your clients are European and use EU standards for measuring energy usage, as well as Celsius instead of Fahrenheit. Do not answer any questions that do not regard energy-efficiency. Try to use the data provided about specific appliances as much as possible. When outputing, DO NOT bold text and DO NOT use lists!"),
                new ChatMessage(ChatMessageRole.User, prompt)
            },
                    MaxTokens = 300
                };

                var chatResult = await openAiApi.Chat.CreateChatCompletionAsync(chatRequest);
                return chatResult.Choices[0].Message.TextContent;//.Trim();
            }
            catch (Exception ex)
            {
                return $"Error retrieving tips: {ex.Message}";
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDashboardSuggestion([FromBody] string userPrompt)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                return BadRequest(new { success = false, message = "Prompt cannot be empty." });
            }

            // 1. Save User's Message
            var userMessageLog = new ChatMessageLog
            {
                UserId = user.Id,
                MessageText = userPrompt,
                Sender = MessageSender.User,
                Timestamp = DateTime.UtcNow // Use UTC
            };
            _context.ChatMessageLogs.Add(userMessageLog);
            // Note: We'll call SaveChangesAsync once after getting the bot's response

            // --- Existing logic to build context for OpenAI ---
            var appliancesList = await _context.Usages
                .Include(u => u.Appliance)
                .Where(u => u.UserId == user.Id)
                .OrderByDescending(u => u.EnergyUsed) // Example: most used
                .ThenByDescending(u => u.UsageFrequency)
                .Take(3) // Limit for prompt conciseness
                .ToListAsync();

            var homesList = await _context.Homes
                .Where(h => h.UserId == user.Id)
                .Take(2) // Limit for prompt conciseness
                .ToListAsync();

            string homeDetails = "My home setup: ";
            if (homesList.Any())
            {
                homeDetails += string.Join("; ", homesList.Select(h =>
                    $"a {h.BuildingType} ('{h.HouseName}') of {h.Size}m^2, insulation: {h.InsulationLevel}, heating: {h.HeatingType}"));
            }
            else
            {
                homeDetails += " general household.";
            }

            string applianceDetails = " Key appliances by usage: ";
            if (appliancesList.Any())
            {
                applianceDetails += string.Join("; ", appliancesList.Select(a =>
                    $"{a.Appliance.Name} ({a.Appliance.PowerRating}, used {a.EnergyUsed} kWh)"));
            }
            else
            {
                applianceDetails += " no specific appliance data to share for this query.";
            }
            // --- End of context building ---

            string finalAiPrompt = $"Context: {homeDetails}. {applianceDetails}. My question is: {userPrompt}";

            var botSuggestion = await GetEnergyEfficiencyTips(finalAiPrompt); // Your existing method

            // 2. Save Bot's Response
            var botMessageLog = new ChatMessageLog
            {
                UserId = user.Id,
                MessageText = botSuggestion,
                Sender = MessageSender.Bot,
                Timestamp = DateTime.UtcNow.AddMilliseconds(1) // Ensure it's after the user's message
            };
            _context.ChatMessageLogs.Add(botMessageLog);

            try
            {
                await _context.SaveChangesAsync(); // Save both user and bot messages
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                return Json(new { success = false, message = "Error saving chat messages." });
            }

            return Json(new { success = true, suggestion = botSuggestion });
        }

        // New Action to retrieve chat history
        [HttpGet]
        public async Task<IActionResult> GetChatHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var history = await _context.ChatMessageLogs
                .Where(m => m.UserId == user.Id)
                .OrderBy(m => m.Timestamp) // Get messages in chronological order
                .Take(50) // Or however many you want to load initially
                .Select(m => new {
                    text = m.MessageText,
                    senderType = m.Sender.ToString().ToLower() // "user" or "bot" for JS
                })
                .ToListAsync();

            return Json(history);
        }


        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Usages";
            var user = await _userManager.GetUserAsync(User);

            var applicationDbContext = _context.Usages
                .Where(u => u.UserId == user.Id)
                .Include(u => u.Appliance)
                .ThenInclude(a => a.Home) 
                .Include(u => u.User);

            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usage = await _context.Usages
                .Include(u => u.Appliance)
                .ThenInclude(a => a.Home) 
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.UsageId == id);

            var user = await _userManager.GetUserAsync(User);
            if (usage == null || usage.UserId != user.Id)
            {
                return Forbid();
            }

            return View(usage);
        }


        public IActionResult Create()
        {
            var user = _userManager.GetUserAsync(User).Result;

            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName");

            ViewData["ApplianceId"] = new SelectList(Enumerable.Empty<SelectListItem>());
            ViewData["UsageFrequencyOptions"] = new Dictionary<int, string>
                {
                    { 1, "Rarely" },
                    { 2, "Sometimes" },
                    { 3, "Often" },
                    { 4, "Very Often" },
                    { 5, "Always" }
                };

            var usage = new Usage
            {
                Date = DateTime.Now.Date,
                Time = DateTime.Now
            };

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,ApplianceId,Date,Time,EnergyUsed,UsageFrequency")] Usage usage)
        {
            ModelState.Remove("User");
            ModelState.Remove("Appliance");
            ModelState.Remove("UserId");

            var user = await _userManager.GetUserAsync(User);
            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(a => a.ApplianceId == usage.ApplianceId);

            if (appliance == null || appliance.Home.UserId != user.Id)
            {
                return Forbid();
            }

            usage.UserId = user.Id;
            usage.Date = usage.Date.Date.Add(usage.Time.TimeOfDay);

            if (ModelState.IsValid)
            {
                _context.Add(usage);
                await _context.SaveChangesAsync();

                // ✅ Inject SignalR Hub Context
                var hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<UsageHub>>();
                await hubContext.Clients.All.SendAsync("ReceiveUsageUpdate", new
                {
                    UsageId = usage.UsageId,
                    ApplianceName = appliance.Name,
                    HomeName = appliance.Home.HouseName,
                    Date = usage.Date.ToString("yyyy-MM-dd"),
                    EnergyUsed = usage.EnergyUsed,
                    UsageFrequency = usage.UsageFrequency
                });


                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = usage.UserId;
            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName");

            ViewData["ApplianceId"] = new SelectList(_context.Appliances.Where(a => a.Home.UserId == user.Id), "ApplianceId", "Name", usage.ApplianceId);
            ViewData["UsageFrequencyOptions"] = new Dictionary<int, string>
                {
                    { 1, "Rarely" },
                    { 2, "Sometimes" },
                    { 3, "Often" },
                    { 4, "Very Often" },
                    { 5, "Always" }
                };
            return View(usage);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usage = await _context.Usages
                .Include(u => u.Appliance)
                .ThenInclude(a => a.Home)
                .FirstOrDefaultAsync(u => u.UsageId == id);

            if (usage == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (usage.UserId != user.Id)
            {
                return Forbid();
            }

            ViewData["SelectedHomeId"] = usage.Appliance.HomeId;
            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName", usage.Appliance.HomeId);
            ViewData["SelectedApplianceId"] = usage.ApplianceId;
            ViewData["UsageFrequencyOptions"] = new Dictionary<int, string>
                {
                    { 1, "Rarely" },
                    { 2, "Sometimes" },
                    { 3, "Often" },
                    { 4, "Very Often" },
                    { 5, "Always" }
                };
            return View(usage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UsageId, UserId,ApplianceId,Date,Time,EnergyUsed,UsageFrequency")] Usage usage)
        {
            ModelState.Remove("User");
            ModelState.Remove("Appliance");
            ModelState.Remove("UserId");

            if (id != usage.UsageId)
            {
                return NotFound();
            }

            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(a => a.ApplianceId == usage.ApplianceId);

            var user = await _userManager.GetUserAsync(User);
            usage.UserId = user.Id;

            if (usage.UserId != user.Id || appliance?.Home.UserId != user.Id)
            {
                return Forbid();
            }

            usage.Date = usage.Date.Date.Add(usage.Time.TimeOfDay); 

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(usage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsageExists(usage.UsageId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = usage.UserId;
            ViewData["ApplianceId"] = new SelectList(_context.Appliances.Where(a => a.Home.UserId == user.Id), "ApplianceId", "Name", usage.ApplianceId);
            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName", usage.Appliance.HomeId);
            ViewData["SelectedApplianceId"] = usage.ApplianceId;
            ViewData["UsageFrequencyOptions"] = new Dictionary<int, string>
                {
                    { 1, "Rarely" },
                    { 2, "Sometimes" },
                    { 3, "Often" },
                    { 4, "Very Often" },
                    { 5, "Always" }
                };
            return View(usage);
        }



        // GET: Usages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usage = await _context.Usages
                .Include(u => u.Appliance)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.UsageId == id);

            var user = await _userManager.GetUserAsync(User);
            if (usage == null || usage.UserId != user.Id)
            {
                return NotFound();
            }

            return View(usage);
        }

        // POST: Usages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usage = await _context.Usages.FindAsync(id);
            if (usage != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (usage.UserId != user.Id)
                {
                    return Forbid(); 
                }

                _context.Usages.Remove(usage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAppliancesByHome(int homeId)
        {
            var user = await _userManager.GetUserAsync(User);

            var appliances = await _context.Appliances
                .Where(a => a.HomeId == homeId && a.Home.UserId == user.Id)
                .Select(a => new
                {
                    a.ApplianceId,
                    a.Name
                })
                .ToListAsync();

            return Json(appliances);
        }
        [HttpGet]
        public async Task<IActionResult> FilterByDate(string date)
        {
            var user = await _userManager.GetUserAsync(User);

            var usagesQuery = _context.Usages
                .Include(u => u.Appliance)
                .ThenInclude(a => a.Home)
                .Where(u => u.UserId == user.Id);

            if (!string.IsNullOrEmpty(date))
            {
                if (DateTime.TryParse(date, out DateTime parsedDate))
                {
                    usagesQuery = usagesQuery.Where(u => u.Date.Date == parsedDate.Date);
                    Console.WriteLine($"Filtering by date: {parsedDate.ToShortDateString()}");
                }
                else
                {
                    Console.WriteLine($"Invalid date format: {date}");
                    return BadRequest("Invalid date format.");
                }
            }


            var filteredUsages = await usagesQuery.ToListAsync();
            if (filteredUsages == null || !filteredUsages.Any())
            {
                return PartialView("~/Views/Shared/_UpdateTableRows.cshtml", filteredUsages);
            }
            return PartialView("~/Views/Shared/_UpdateTableRows.cshtml", filteredUsages);

        }

        private bool UsageExists(int id)
        {
            return _context.Usages.Any(e => e.UsageId == id);
        }
    }
}
