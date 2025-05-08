using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EffiSense.Controllers
{
    [Authorize]
    public class AppliancesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PageSize = 9;

        public AppliancesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Appliances
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Appliances";
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var appliancesQuery = _context.Appliances
                .Where(a => a.Home.UserId == user.Id)
                .Include(a => a.Home)
                .OrderBy(a => a.Name);

            var appliances = await appliancesQuery
                .Take(PageSize) 
                .ToListAsync();

            ViewBag.CurrentPage = 1;
            ViewBag.HasMoreItems = await appliancesQuery.Skip(PageSize).AnyAsync(); 
            ViewBag.PageSize = PageSize;

            return View(appliances);
        }

        // GET: Appliances/LoadMoreAppliances
        [HttpGet]
        public async Task<IActionResult> LoadMoreAppliances(int pageNumber = 2)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(); 

            int itemsToSkip = (pageNumber - 1) * PageSize;

            var appliances = await _context.Appliances
                .Where(a => a.Home.UserId == user.Id)
                .Include(a => a.Home)
                .OrderBy(a => a.Name) 
                .Skip(itemsToSkip)
                .Take(PageSize)
                .ToListAsync();

            bool hasMore = await _context.Appliances
                                     .Where(a => a.Home.UserId == user.Id)
                                     .Skip(itemsToSkip + PageSize)
                                     .AnyAsync();

            ViewBag.HasMoreItems = hasMore; 

            if (!appliances.Any())
            {
                return Content(""); 
            }

            return PartialView("_ApplianceGridItems", appliances);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(m => m.ApplianceId == id && m.Home.UserId == user.Id);

            if (appliance == null)
            {
                return NotFound(); 
            }

            return View(appliance);
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }
            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("HomeId,Name,Brand,PowerRating,EfficiencyRating,Notes,PurchaseDate,IconClass")] Appliance appliance)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            ModelState.Remove("Home");

            var home = await _context.Homes.FirstOrDefaultAsync(h => h.HomeId == appliance.HomeId && h.UserId == user.Id);
            if (home == null)
            {
                ModelState.AddModelError("HomeId", "Invalid home selection.");
                ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName", appliance.HomeId);
                return View(appliance);
            }

            if (ModelState.IsValid)
            {
                _context.Add(appliance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName", appliance.HomeId);
            return View(appliance);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appliance = await _context.Appliances
                                      .Include(a => a.Home) 
                                      .FirstOrDefaultAsync(a => a.ApplianceId == id);

            if (appliance == null)
            {
                return NotFound();
            }

            if (appliance.Home == null || appliance.Home.UserId != user.Id)
            {
                return Forbid();
            }

            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName", appliance.HomeId);
            return View(appliance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("ApplianceId,HomeId,Name,Brand,PowerRating,EfficiencyRating,Notes,PurchaseDate,IconClass")] Appliance appliance)
        {
            if (id != appliance.ApplianceId)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var applianceToUpdate = await _context.Appliances
                                               .Include(a => a.Home)
                                               .FirstOrDefaultAsync(a => a.ApplianceId == id);

            if (applianceToUpdate == null)
            {
                return NotFound();
            }

            var newHome = await _context.Homes.FirstOrDefaultAsync(h => h.HomeId == appliance.HomeId && h.UserId == user.Id);
            if (applianceToUpdate.Home.UserId != user.Id || newHome == null)
            {
                return Forbid();
            }

            ModelState.Remove("Home");

            if (ModelState.IsValid)
            {
                try
                {
                    applianceToUpdate.HomeId = appliance.HomeId;
                    applianceToUpdate.Name = appliance.Name;
                    applianceToUpdate.Brand = appliance.Brand;
                    applianceToUpdate.PowerRating = appliance.PowerRating;
                    applianceToUpdate.EfficiencyRating = appliance.EfficiencyRating;
                    applianceToUpdate.Notes = appliance.Notes;
                    applianceToUpdate.PurchaseDate = appliance.PurchaseDate;
                    applianceToUpdate.IconClass = appliance.IconClass;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplianceExists(appliance.ApplianceId))
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
            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName", appliance.HomeId);
            return View(appliance);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(m => m.ApplianceId == id && m.Home.UserId == user.Id);

            if (appliance == null)
            {
                return NotFound(); 
            }

            return View(appliance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var appliance = await _context.Appliances
                                      .Include(a => a.Home)
                                      .FirstOrDefaultAsync(a => a.ApplianceId == id);

            if (appliance != null)
            {
                if (appliance.Home == null || appliance.Home.UserId != user.Id)
                {
                    return Forbid();
                }

                var usages = _context.Usages.Where(u => u.ApplianceId == id && u.UserId == user.Id);
                if (usages.Any())
                {
                    _context.Usages.RemoveRange(usages);
                }

                _context.Appliances.Remove(appliance);
                await _context.SaveChangesAsync();
            }
            else
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ApplianceExists(int id)
        {
            return _context.Appliances.Any(e => e.ApplianceId == id);
        }
    }
}
