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
    public class HomesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var homes = await _context.Homes
                .Where(h => h.UserId == user.Id)
                .ToListAsync();

            return View(homes);
        }

        // GET: Homes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var home = await _context.Homes
                .Include(h => h.User)
                .FirstOrDefaultAsync(m => m.HomeId == id);
            if (home == null)
            {
                return NotFound();
            }

            return View(home);
        }

        // GET: Homes/Create
        public IActionResult Create()
        {
            var user = _userManager.GetUserAsync(User).Result;

            ViewData["UserId"] = user.Id;

            return View();
        }

        // POST: Homes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HomeId,Size,HeatingType,NumberOfAppliances")] Home home)
        {
            ModelState.Remove("User");
            ModelState.Remove("Appliances");
            ModelState.Remove("UserId"); 

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                home.UserId = user.Id;

                _context.Add(home);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = home.UserId;
            return View(home);
        }

        // GET: Homes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var home = await _context.Homes.FindAsync(id);
            if (home == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (home.UserId != user.Id)
            {
                return Forbid();
            }

            ViewData["UserId"] = home.UserId;
            return View(home);
        }


        // POST: Homes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HomeId,Size,HeatingType,NumberOfAppliances")] Home home)
        {
            ModelState.Remove("User"); 
            ModelState.Remove("Appliances"); 
            ModelState.Remove("UserId"); 

            if (id != home.HomeId)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            home.UserId = user.Id;

            if (home.UserId != user.Id) 
            {
                return Forbid(); 
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(home);
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

            ViewData["UserId"] = home.UserId;
            return View(home);
        }


        // GET: Homes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var home = await _context.Homes
                .Include(h => h.User)
                .FirstOrDefaultAsync(m => m.HomeId == id);
            if (home == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (home.UserId != user.Id) 
            {
                return Forbid();
            }

            return View(home);
        }

        // POST: Homes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var home = await _context.Homes.FindAsync(id);
            if (home != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (home.UserId != user.Id) 
                {
                    return Forbid();
                }

                _context.Homes.Remove(home);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private bool HomeExists(int id)
        {
            return _context.Homes.Any(e => e.HomeId == id);
        }
    }
}