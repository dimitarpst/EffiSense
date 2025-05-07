using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EffiSense.Data; // Assuming your DbContext and Models are here or in EffiSense.Models
using EffiSense.Models;

namespace EffiSense.Controllers
{
    [Authorize]
    public class AppliancesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppliancesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Appliances";
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Or another appropriate response if user is not found
            }

            var appliances = await _context.Appliances
                .Where(a => a.Home.UserId == user.Id)
                .Include(a => a.Home)
                .ToListAsync();

            return View(appliances);
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
                return NotFound(); // Or Forbid() if you want to distinguish
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

            // Explicitly remove "Home" from ModelState if it's causing issues,
            // as we are only binding HomeId and will load Home separately if needed for validation.
            ModelState.Remove("Home");

            var home = await _context.Homes.FirstOrDefaultAsync(h => h.HomeId == appliance.HomeId && h.UserId == user.Id);
            if (home == null)
            {
                // User is trying to assign appliance to a home they don't own or that doesn't exist
                ModelState.AddModelError("HomeId", "Invalid home selection.");
                ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HouseName", appliance.HomeId);
                return View(appliance);
            }

            if (ModelState.IsValid)
            {
                // Home is valid and belongs to the user, proceed to add appliance
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
                                      .Include(a => a.Home) // Include Home to check UserId
                                      .FirstOrDefaultAsync(a => a.ApplianceId == id);

            if (appliance == null)
            {
                return NotFound();
            }

            // Security check: Ensure the appliance belongs to a home owned by the current user
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

            // Fetch the existing appliance from DB to check ownership and update
            var applianceToUpdate = await _context.Appliances
                                               .Include(a => a.Home)
                                               .FirstOrDefaultAsync(a => a.ApplianceId == id);

            if (applianceToUpdate == null)
            {
                return NotFound();
            }

            // Security check: Ensure the appliance belongs to a home owned by the current user
            // Also check if the new HomeId (if changed) belongs to the user
            var newHome = await _context.Homes.FirstOrDefaultAsync(h => h.HomeId == appliance.HomeId && h.UserId == user.Id);
            if (applianceToUpdate.Home.UserId != user.Id || newHome == null)
            {
                return Forbid();
            }

            // Remove "Home" from ModelState if it's causing issues, as it's a navigation property
            // and we're binding HomeId.
            ModelState.Remove("Home");

            if (ModelState.IsValid)
            {
                try
                {
                    // Manually update properties on the entity tracked by EF Core
                    // This is safer than _context.Update(appliance) if appliance is not tracked
                    // or if you want to prevent overposting of certain properties not in Bind.
                    applianceToUpdate.HomeId = appliance.HomeId;
                    applianceToUpdate.Name = appliance.Name;
                    applianceToUpdate.Brand = appliance.Brand;
                    applianceToUpdate.PowerRating = appliance.PowerRating;
                    applianceToUpdate.EfficiencyRating = appliance.EfficiencyRating;
                    applianceToUpdate.Notes = appliance.Notes;
                    applianceToUpdate.PurchaseDate = appliance.PurchaseDate;
                    applianceToUpdate.IconClass = appliance.IconClass;

                    // _context.Update(applianceToUpdate); // Or just let EF Core track changes
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
                return NotFound(); // Or Forbid()
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
                                      .Include(a => a.Home) // Include Home to check UserId
                                      .FirstOrDefaultAsync(a => a.ApplianceId == id);

            if (appliance != null)
            {
                // Security check
                if (appliance.Home == null || appliance.Home.UserId != user.Id)
                {
                    return Forbid();
                }

                // Find and remove related usages
                var usages = _context.Usages.Where(u => u.ApplianceId == id && u.UserId == user.Id); // Ensure user owns usages too
                if (usages.Any())
                {
                    _context.Usages.RemoveRange(usages);
                }

                _context.Appliances.Remove(appliance);
                await _context.SaveChangesAsync();
            }
            else
            {
                return NotFound(); // Appliance to delete was not found
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ApplianceExists(int id)
        {
            // Consider adding a user check here if appliances are user-specific
            // For now, it just checks existence.
            return _context.Appliances.Any(e => e.ApplianceId == id);
        }
    }
}
