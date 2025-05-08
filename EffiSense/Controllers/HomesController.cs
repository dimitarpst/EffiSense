using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EffiSense.Controllers
{
    [Authorize]
    public class HomesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int PageSize = 9;

        public HomesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "My Homes";
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var homesQuery = _context.Homes
                .Where(h => h.UserId == user.Id)
                .OrderBy(h => h.HouseName);

            var homes = await homesQuery
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentPage = 1;
            ViewBag.PageSize = PageSize;
            ViewBag.HasMoreItems = await homesQuery.Skip(PageSize).AnyAsync();

            return View(homes);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMoreHomes(int pageNumber = 2)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); 
            }

            int itemsToSkip = (pageNumber - 1) * PageSize;

            var homesQuery = _context.Homes
                .Where(h => h.UserId == user.Id)
                .OrderBy(h => h.HouseName); 

            var homes = await homesQuery
                .Skip(itemsToSkip)
                .Take(PageSize)
                .ToListAsync();

            Response.Headers.Append("X-HasMoreItems", (await homesQuery.Skip(itemsToSkip + PageSize).AnyAsync()).ToString().ToLower());

            if (!homes.Any())
            {
                return Content(""); 
            }

            return PartialView("../Shared/_HomeGridItems", homes); 
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

            var home = await _context.Homes
                .FirstOrDefaultAsync(m => m.HomeId == id && m.UserId == user.Id);

            if (home == null)
            {
                return NotFound();
            }

            return View(home);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("HouseName,Size,HeatingType,Location,Address,BuildingType,InsulationLevel,YearBuilt,Description")] Home home)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            home.UserId = user.Id;

            ModelState.Remove("User");
            ModelState.Remove("Appliances");

            if (ModelState.IsValid)
            {
                _context.Add(home);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(home);
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

            var home = await _context.Homes.FirstOrDefaultAsync(h => h.HomeId == id && h.UserId == user.Id);
            if (home == null)
            {
                return NotFound();
            }
            return View(home);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("HomeId,UserId,HouseName,Size,HeatingType,Location,Address,BuildingType,InsulationLevel,YearBuilt,Description")] Home home)
        {
            if (id != home.HomeId)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (home.UserId != user.Id)
            {
                return Forbid();
            }

            ModelState.Remove("User");
            ModelState.Remove("Appliances");

            if (ModelState.IsValid)
            {
                try
                {
                    var homeToUpdate = await _context.Homes.FirstOrDefaultAsync(h => h.HomeId == id && h.UserId == user.Id);
                    if (homeToUpdate == null)
                    {
                        return NotFound();
                    }

                    homeToUpdate.HouseName = home.HouseName;
                    homeToUpdate.Size = home.Size;
                    homeToUpdate.HeatingType = home.HeatingType;
                    homeToUpdate.Location = home.Location;
                    homeToUpdate.Address = home.Address;
                    homeToUpdate.BuildingType = home.BuildingType;
                    homeToUpdate.InsulationLevel = home.InsulationLevel;
                    homeToUpdate.YearBuilt = home.YearBuilt;
                    homeToUpdate.Description = home.Description;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HomeExists(home.HomeId))
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
            return View(home);
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

            var home = await _context.Homes
                .FirstOrDefaultAsync(m => m.HomeId == id && m.UserId == user.Id);

            if (home == null)
            {
                return NotFound();
            }

            return View(home);
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

            var home = await _context.Homes.FirstOrDefaultAsync(h => h.HomeId == id && h.UserId == user.Id);
            if (home != null)
            {
                var applianceIds = await _context.Appliances
                                               .Where(a => a.HomeId == id)
                                               .Select(a => a.ApplianceId)
                                               .ToListAsync();

                if (applianceIds.Any())
                {
                    var usages = await _context.Usages
                                           .Where(u => applianceIds.Contains(u.ApplianceId) && u.UserId == user.Id)
                                           .ToListAsync();
                    if (usages.Any())
                    {
                        _context.Usages.RemoveRange(usages);
                    }

                    var appliances = await _context.Appliances
                                               .Where(a => applianceIds.Contains(a.ApplianceId))
                                               .ToListAsync();
                    if (appliances.Any())
                    {
                        _context.Appliances.RemoveRange(appliances);
                    }
                }

                _context.Homes.Remove(home);
                await _context.SaveChangesAsync();
            }
            else
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool HomeExists(int id)
        {
            return _context.Homes.Any(e => e.HomeId == id);
        }
    }
}
