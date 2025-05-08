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
        private readonly IHubContext<UsageHub> _hubContext;
        private const int PageSize = 12;

        public UsagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, IHubContext<UsageHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        private async Task<string> GetEnergyEfficiencyTips(string prompt)
        {
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    // _logger.LogError("OpenAI API Key is not configured."); // Optional: Log this as well
                    return "Error: OpenAI API Key is not configured.";
                }
                var openAiApi = new OpenAIAPI(apiKey); 
                var chatRequest = new ChatRequest
                {
                    Model = "gpt-3.5-turbo",
                    Messages = new[] {
                new ChatMessage(ChatMessageRole.System, "You are an assistant that provides energy-efficiency tips. Your clients are European and use EU standards for measuring energy usage, as well as Celsius instead of Fahrenheit. The user will provide details about their home setup (like building type, size, year built, insulation, heating, and a brief description) and key appliances (including name, power rating, efficiency rating, energy used, and context notes for usage). Try to use all the provided data about their home and specific appliances as much as possible to give targeted advice. Do not answer any questions that do not regard energy-efficiency. You can use Markdown for formatting your response, including bold text using **text** and unordered lists where each item starts with a hyphen (-) or an asterisk (*). Please ensure each list item is on a new line."),
                new ChatMessage(ChatMessageRole.User, prompt)
            },
                    MaxTokens = 500 
                };

                var chatResult = await openAiApi.Chat.CreateChatCompletionAsync(chatRequest);

                if (chatResult?.Choices?.Count > 0 && !string.IsNullOrWhiteSpace(chatResult.Choices[0].Message?.TextContent))
                {
                    return chatResult.Choices[0].Message.TextContent.Trim();
                }
                else
                {
                    // _logger.LogWarning("OpenAI response was empty or invalid for prompt: {prompt}", prompt); 
                    return "Sorry, I couldn't generate a tip right now.";
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error in GetEnergyEfficiencyTips for prompt: {prompt}", prompt); 
                return $"Error retrieving tips. Please check configuration or try again later.";
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDashboardSuggestion([FromBody] string userPrompt)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) { return Unauthorized(new { success = false, message = "User not authenticated." }); }
            if (string.IsNullOrWhiteSpace(userPrompt)) { return BadRequest(new { success = false, message = "Prompt cannot be empty." }); }

            var userMessageLog = new ChatMessageLog { UserId = user.Id, MessageText = userPrompt, Sender = MessageSender.User, Timestamp = DateTime.UtcNow };
            _context.ChatMessageLogs.Add(userMessageLog);

            var appliancesList = await _context.Usages
                .Where(u => u.UserId == user.Id && u.Appliance != null)
                .OrderByDescending(u => u.EnergyUsed).ThenByDescending(u => u.UsageFrequency)
                .Select(u => new {
                    u.Appliance.Name,
                    u.Appliance.PowerRating,
                    u.Appliance.EfficiencyRating, 
                    u.EnergyUsed,
                    u.ContextNotes 
                }).Take(3).ToListAsync();

            var homesList = await _context.Homes
                .Where(h => h.UserId == user.Id)
                .Select(h => new {
                    h.BuildingType,
                    h.HouseName,
                    h.Size,
                    h.InsulationLevel,
                    h.HeatingType,
                    h.YearBuilt, 
                    h.Description 
                }).Take(2).ToListAsync();

            string homeDetails = "My home setup: ";
            homeDetails += homesList.Any() ? string.Join("; ", homesList.Select(h =>
                $"a {h.BuildingType ?? "N/A"} ('{h.HouseName ?? "N/A"}') of {h.Size}m^2, " +
                $"built around {h.YearBuilt?.ToString() ?? "N/A"}, " +
                $"insulation: {h.InsulationLevel ?? "N/A"}, heating: {h.HeatingType ?? "N/A"}" +
                (!string.IsNullOrWhiteSpace(h.Description) ? $", Description: {TruncateString(h.Description, 100)}" : "")
            )) : " general household.";

            string applianceDetails = " Key appliances by usage: ";
            applianceDetails += appliancesList.Any() ? string.Join("; ", appliancesList.Select(a =>
                $"{a.Name ?? "N/A"} ({a.PowerRating ?? "N/A"}" +
                (!string.IsNullOrWhiteSpace(a.EfficiencyRating) ? $", Efficiency: {a.EfficiencyRating}" : "") +
                $", used {a.EnergyUsed} kWh" +
                (!string.IsNullOrWhiteSpace(a.ContextNotes) ? $", Notes: {TruncateString(a.ContextNotes, 100)}" : "") +
                ")"
            )) : " no specific appliance data to share for this query.";

            string finalAiPrompt = $"Context: {homeDetails}. {applianceDetails}. My question is: {userPrompt}";

            var botSuggestion = await GetEnergyEfficiencyTips(finalAiPrompt); 
            var botMessageLog = new ChatMessageLog { UserId = user.Id, MessageText = botSuggestion, Sender = MessageSender.Bot, Timestamp = DateTime.UtcNow.AddMilliseconds(1) };
            _context.ChatMessageLogs.Add(botMessageLog);

            try { await _context.SaveChangesAsync(); }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving chat messages." });
            }

            return Json(new { success = true, suggestion = botSuggestion });
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var history = await _context.ChatMessageLogs
                .Where(m => m.UserId == user.Id).OrderBy(m => m.Timestamp).Take(50)
                .Select(m => new { text = m.MessageText, senderType = m.Sender.ToString().ToLower() }).ToListAsync();
            return Json(history);
        }

        public async Task<IActionResult> Index(string dateFilter = null, int pageNumber = 1)
        {
            ViewData["Title"] = "My Energy Usages";
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            IQueryable<Usage> usagesQuery = _context.Usages
                                                .Where(u => u.UserId == user.Id)
                                                .Include(u => u.Appliance)
                                                    .ThenInclude(a => a.Home);

            if (!string.IsNullOrEmpty(dateFilter) && DateTime.TryParse(dateFilter, out DateTime parsedDate))
            {
                usagesQuery = usagesQuery.Where(u => u.Date.Date == parsedDate.Date);
                ViewBag.CurrentFilter = dateFilter;
            }
            else
            {
                ViewBag.CurrentFilter = string.Empty;
            }

            usagesQuery = usagesQuery.OrderByDescending(u => u.Date);

            var totalItems = await usagesQuery.CountAsync();
            var usages = await usagesQuery
                                .Skip((pageNumber - 1) * PageSize)
                                .Take(PageSize)
                                .ToListAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = PageSize;
            ViewBag.HasMoreItems = (pageNumber * PageSize) < totalItems;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" && !(ViewData["IsFullPageLoadForFilter"]?.ToString() == "true"))
            {
                return PartialView("_UsagesGridPartial", usages);
            }

            return View(usages);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMoreUsages(int pageNumber = 2, string dateFilter = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            IQueryable<Usage> usagesQuery = _context.Usages
                                                .Where(u => u.UserId == user.Id)
                                                .Include(u => u.Appliance)
                                                    .ThenInclude(a => a.Home);

            if (!string.IsNullOrEmpty(dateFilter) && DateTime.TryParse(dateFilter, out DateTime parsedDate))
            {
                usagesQuery = usagesQuery.Where(u => u.Date.Date == parsedDate.Date);
            }

            usagesQuery = usagesQuery.OrderByDescending(u => u.Date);

            int itemsToSkip = (pageNumber - 1) * PageSize;
            var usages = await usagesQuery
                                .Skip(itemsToSkip)
                                .Take(PageSize)
                                .ToListAsync();

            var totalRemainingItems = await usagesQuery.CountAsync(); 
            bool hasMore = (pageNumber * PageSize) < totalRemainingItems;

            Response.Headers.Append("X-HasMoreItems", hasMore.ToString().ToLower());

            if (!usages.Any())
            {
                return Content("");
            }

            return PartialView("../Shared/_UsageGridItems", usages);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var usage = await _context.Usages
                .Include(u => u.Appliance).ThenInclude(a => a.Home)
                .FirstOrDefaultAsync(m => m.UsageId == id && m.UserId == user.Id);

            if (usage == null) return NotFound();
            return View(usage);
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewData["ApplianceId"] = new SelectList(
                _context.Appliances.Where(a => a.Home.UserId == user.Id).Include(a => a.Home).OrderBy(a => a.Home.HouseName).ThenBy(a => a.Name),
                "ApplianceId", "Name", null, "Home.HouseName");

            var usage = new Usage { Date = DateTime.Now.Date, Time = DateTime.Now };
            return View(usage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ApplianceId,Date,Time,EnergyUsed,UsageFrequency,ContextNotes,IconClass")] Usage usage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            usage.UserId = user.Id;
            usage.Date = usage.Date.Date.Add(usage.Time.TimeOfDay);

            ModelState.Remove("User");
            ModelState.Remove("Appliance");

            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(a => a.ApplianceId == usage.ApplianceId && a.Home.UserId == user.Id);

            if (appliance == null) { ModelState.AddModelError("ApplianceId", "Invalid appliance selection."); }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(usage.IconClass) && appliance != null)
                {
                    usage.IconClass = appliance.IconClass;
                }

                _context.Add(usage);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveUsageUpdate", new
                {
                    usage.UsageId,
                    ApplianceName = appliance?.Name ?? "N/A",
                    HomeName = appliance?.Home?.HouseName ?? "N/A",
                    Date = usage.Date.ToString("yyyy-MM-dd HH:mm"),
                    usage.EnergyUsed,
                    usage.UsageFrequency,
                    usage.ContextNotes,
                    IconClass = usage.IconClass 
                });

                return RedirectToAction(nameof(Index));
            }

            ViewData["ApplianceId"] = new SelectList(
                 _context.Appliances.Where(a => a.Home.UserId == user.Id).Include(a => a.Home).OrderBy(a => a.Home.HouseName).ThenBy(a => a.Name),
                "ApplianceId", "Name", usage.ApplianceId, "Home.HouseName");
            return View(usage);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var usage = await _context.Usages
                .Include(u => u.Appliance).ThenInclude(a => a.Home)
                .FirstOrDefaultAsync(u => u.UsageId == id && u.UserId == user.Id);

            if (usage == null) return NotFound();

            ViewData["ApplianceId"] = new SelectList(
                 _context.Appliances.Where(a => a.Home.UserId == user.Id).Include(a => a.Home).OrderBy(a => a.Home.HouseName).ThenBy(a => a.Name),
                "ApplianceId", "Name", usage.ApplianceId, "Home.HouseName");

            usage.Time = DateTime.MinValue.Add(usage.Date.TimeOfDay);
            return View(usage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("UsageId,UserId,ApplianceId,Date,Time,EnergyUsed,UsageFrequency,ContextNotes,IconClass")] Usage usage)
        {
            if (id != usage.UsageId) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (usage.UserId != user.Id) return Forbid();

            usage.Date = usage.Date.Date.Add(usage.Time.TimeOfDay);

            ModelState.Remove("User");
            ModelState.Remove("Appliance");

            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(a => a.ApplianceId == usage.ApplianceId && a.Home.UserId == user.Id);

            if (appliance == null) { ModelState.AddModelError("ApplianceId", "Invalid appliance selection."); }

            if (ModelState.IsValid)
            {
                try
                {
                    var usageToUpdate = await _context.Usages.FirstOrDefaultAsync(u => u.UsageId == id && u.UserId == user.Id);
                    if (usageToUpdate == null) return NotFound();

                    usageToUpdate.ApplianceId = usage.ApplianceId;
                    usageToUpdate.Date = usage.Date;
                    usageToUpdate.EnergyUsed = usage.EnergyUsed;
                    usageToUpdate.UsageFrequency = usage.UsageFrequency;
                    usageToUpdate.ContextNotes = usage.ContextNotes;
                    usageToUpdate.IconClass = usage.IconClass;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsageExists(usage.UsageId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ApplianceId"] = new SelectList(
                 _context.Appliances.Where(a => a.Home.UserId == user.Id).Include(a => a.Home).OrderBy(a => a.Home.HouseName).ThenBy(a => a.Name),
                "ApplianceId", "Name", usage.ApplianceId, "Home.HouseName");
            return View(usage);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var usage = await _context.Usages
                .Include(u => u.Appliance).ThenInclude(a => a.Home)
                .FirstOrDefaultAsync(m => m.UsageId == id && m.UserId == user.Id);

            if (usage == null) return NotFound();
            return View(usage);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var usage = await _context.Usages.FirstOrDefaultAsync(u => u.UsageId == id && u.UserId == user.Id);
            if (usage != null)
            {
                _context.Usages.Remove(usage);
                await _context.SaveChangesAsync();
            }
            else { return NotFound(); }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAppliancesByHome(int homeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var homeExists = await _context.Homes.AnyAsync(h => h.HomeId == homeId && h.UserId == user.Id);
            if (!homeExists) return Forbid();

            var appliances = await _context.Appliances
                .Where(a => a.HomeId == homeId)
                .Select(a => new { a.ApplianceId, a.Name })
                .OrderBy(a => a.Name).ToListAsync();
            return Json(appliances);
        }

        [HttpGet]
        public async Task<IActionResult> FilterByDate(string dateFilter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            IQueryable<Usage> usagesQuery = _context.Usages.Where(u => u.UserId == user.Id);

            if (!string.IsNullOrEmpty(dateFilter) && DateTime.TryParse(dateFilter, out DateTime parsedDate))
            {
                usagesQuery = usagesQuery.Where(u => u.Date.Date == parsedDate.Date);
            }

            var filteredUsages = await usagesQuery
                .Include(u => u.Appliance).ThenInclude(a => a.Home)
                .OrderByDescending(u => u.Date).ThenByDescending(u => u.Time)
                .ToListAsync();

            return PartialView("_UsagesGridPartial", filteredUsages);
        }

        private bool UsageExists(int id)
        {
            return _context.Usages.Any(e => e.UsageId == id);
        }
    }
}
