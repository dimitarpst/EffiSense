using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
                new ChatMessage(ChatMessageRole.System, "You are an assistant that provides energy-efficiency tips."),
                new ChatMessage(ChatMessageRole.User, prompt)
            },
                    MaxTokens = 150
                };

                var chatResult = await openAiApi.Chat.CreateChatCompletionAsync(chatRequest);
                return chatResult.Choices[0].Message.Content.Trim();
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

            var topUsage = await _context.Usages
                .Include(u => u.Appliance)
                .Where(u => u.UserId == user.Id)
                .OrderByDescending(u => u.EnergyUsed)
                .FirstOrDefaultAsync();

            if (topUsage == null)
            {
                return Json(new { success = false, message = "No usage data available." });
            }

            string aiPrompt = $"Based on my energy usage, my most energy-intensive device is {topUsage.Appliance.Name} which consumes {topUsage.EnergyUsed} kWh daily. {userPrompt} Provide a suggestion to optimize my energy consumption.";

            var suggestion = await GetEnergyEfficiencyTips(aiPrompt);

            return Json(new { success = true, suggestion });
        }

        public async Task<IActionResult> Index()
        {
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

            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "Size");

            ViewData["ApplianceId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UsageId,UserId,ApplianceId,Date,Time,EnergyUsed,UsageFrequency,Duration")] Usage usage)
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
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = usage.UserId;
            ViewData["ApplianceId"] = new SelectList(_context.Appliances.Where(a => a.Home.UserId == user.Id), "ApplianceId", "Name", usage.ApplianceId);
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
            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "Size", usage.Appliance.HomeId);
            ViewData["SelectedApplianceId"] = usage.ApplianceId; 
            return View(usage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UsageId,UserId,ApplianceId,Date,Time,EnergyUsed,UsageFrequency,Duration")] Usage usage)
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
        public async Task<IActionResult> FilterByDate(DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);

            var usagesQuery = _context.Usages
                .Include(u => u.Appliance)
                .ThenInclude(a => a.Home)
                .Where(u => u.UserId == user.Id);

            if (date.HasValue)
            {
                usagesQuery = usagesQuery.Where(u => u.Date.Date == date.Value.Date);
            }

            var filteredUsages = await usagesQuery.ToListAsync();
            return PartialView("_UsageTableRows", filteredUsages);
        }




        private bool UsageExists(int id)
        {
            return _context.Usages.Any(e => e.UsageId == id);
        }
    }
}
