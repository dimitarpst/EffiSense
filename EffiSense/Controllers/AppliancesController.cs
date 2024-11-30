using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public AppliancesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var appliances = await _context.Appliances
                .Where(a => a.Home.UserId == user.Id)
                .ToListAsync();

            return View(appliances);
        }

        // GET: Appliances/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(m => m.ApplianceId == id);
            if (appliance == null)
            {
                return NotFound();
            }

            return View(appliance);
        }

        public IActionResult Create()
        {
            var user = _userManager.GetUserAsync(User).Result;

            var homes = _context.Homes.Where(h => h.UserId == user.Id).ToList();

            ViewData["HomeId"] = new SelectList(homes, "HomeId", "HomeId");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApplianceId,HomeId,Name,Brand,IsActive,PowerRating")] Appliance appliance)
        {
            ModelState.Remove("Home");

            var user = await _userManager.GetUserAsync(User);
            var home = await _context.Homes.FindAsync(appliance.HomeId);

            if (home == null || home.UserId != user.Id)
            {
                return Forbid();  
            }

            if (ModelState.IsValid)
            {
                _context.Add(appliance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var homes = _context.Homes.Where(h => h.UserId == user.Id).ToList();
            ViewData["HomeId"] = new SelectList(homes, "HomeId", "HomeId", appliance.HomeId);
            return View(appliance);
        }


        // GET: Appliances/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appliance = await _context.Appliances.FindAsync(id);
            if (appliance == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var home = await _context.Homes.FindAsync(appliance.HomeId);

            if (home == null || home.UserId != user.Id)
            {
                return Forbid();
            }

            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HomeId", appliance.HomeId);
            return View(appliance);
        }



        // POST: Appliances/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ApplianceId,HomeId,Name,Brand,IsActive,PowerRating")] Appliance appliance)
        {
            ModelState.Remove("Home");

            if (id != appliance.ApplianceId)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var home = await _context.Homes.FindAsync(appliance.HomeId);

            if (home == null || home.UserId != user.Id)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appliance);
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

            ViewData["HomeId"] = new SelectList(_context.Homes.Where(h => h.UserId == user.Id), "HomeId", "HomeId", appliance.HomeId);
            return View(appliance);
        }


        // GET: Appliances/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appliance = await _context.Appliances
                .Include(a => a.Home)
                .FirstOrDefaultAsync(m => m.ApplianceId == id);

            if (appliance == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (appliance.Home.UserId != user.Id)
            {
                return Forbid(); 
            }

            return View(appliance);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appliance = await _context.Appliances.FindAsync(id);
            if (appliance != null)
            {
                var user = await _userManager.GetUserAsync(User);
                var home = await _context.Homes.FindAsync(appliance.HomeId);

                if (home.UserId != user.Id)
                {
                    return Forbid();
                }

                var usages = _context.Usages.Where(u => u.ApplianceId == id);
                _context.Usages.RemoveRange(usages);

                _context.Appliances.Remove(appliance);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplianceExists(int id)
        {
            return _context.Appliances.Any(e => e.ApplianceId == id);
        }

    }
}