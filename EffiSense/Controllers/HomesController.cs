using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EffiSense.Controllers
{
    [Authorize]
    public class HomesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomesController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        // GET: Homes
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var homes = User.IsInRole("Administrator")
                ? await _context.Homes.ToListAsync() 
                : await _context.Homes
                    .Where(h => h.UserId == userId)  
                    .ToListAsync();

            return View(homes);
        }

        // GET: Homes/Details/5
       // GET: Homes/Details/5
public async Task<IActionResult> Details(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var home = User.IsInRole("Administrator")
        ? await _context.Homes.FindAsync(id) // Ако е администратор, няма филтър по UserId
        : await _context.Homes
            .FirstOrDefaultAsync(h => h.HomeId == id && h.UserId == userId); // Ако не е администратор, филтрираме по UserId

    if (home == null)
    {
        return Unauthorized(); // Ако няма достъп (не е собственик и не е администратор)
    }

    return View(home);
}


        // GET: Homes/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Homes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HomeId,UserId,Size,HeatingType,NumberOfAppliances")] Home home)
        {
            ModelState.Remove("User");
            ModelState.Remove("Appliances");
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                home.UserId = userId; 

                _context.Add(home);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", home.UserId);
            return View(home);
        }

        // GET: Homes/Edit/5
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

            // Ако е администратор, може да редактира всеки дом
            if (User.IsInRole("Administrator"))
            {
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", home.UserId); // Позволява администраторът да избира потребител
            }
            else
            {
                // Ако не е администратор, той може да редактира само собствения си дом
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", home.UserId);
            }

            return View(home);
        }

        public double CalculateEnergyConsumption(int homeId)
        {
            var appliances = _context.Appliances
                .Where(a => a.HomeId == homeId && a.IsActive);
            return appliances.Sum(a => a.EnergyConsumption);
        }

        public async Task<IActionResult> EnergyUsage(int id)
        {
            
            var appliances = await _context.Appliances
                .Where(a => a.HomeId == id)
                .ToListAsync();

       
            var energyConsumption = EnergyHelper.CalculateEnergy(appliances);

           
            ViewBag.EnergyConsumption = energyConsumption;
            return View();
        }
        // POST: Homes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HomeId,UserId,Size,HeatingType,NumberOfAppliances")] Home home)
        {
            ModelState.Remove("User");
            ModelState.Remove("Appliances");
            if (id != home.HomeId)
            {
                return NotFound();
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", home.UserId);
            return View(home);
        }

        // GET: Homes/Delete/5
        // GET: Homes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var home = User.IsInRole("Administrator")
                ? await _context.Homes.Include(h => h.User).FirstOrDefaultAsync(m => m.HomeId == id)
                : await _context.Homes
                    .Include(h => h.User)
                    .FirstOrDefaultAsync(m => m.HomeId == id && m.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (home == null)
            {
                return NotFound();
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
                _context.Homes.Remove(home);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EnergyChart()
        {
            var data = await _context.Homes
                .Include(h => h.Appliances)
                .Select(h => new
                {
                    HomeName = h.HeatingType,
                    TotalConsumption = h.Appliances.Where(a => a.IsActive).Sum(a => a.EnergyConsumption)
                })
                .ToListAsync();

            ViewBag.ChartData = data;
            return View();
        }

        private bool HomeExists(int id)
        {
            return _context.Homes.Any(e => e.HomeId == id);
        }
    }
}
