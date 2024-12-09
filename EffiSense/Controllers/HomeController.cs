using EffiSense.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EffiSense.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
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

            var dayOfWeekData = _context.Usages
                .Where(u => u.UserId == user.Id)
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

            var monthlyUsageData = _context.Usages
                .Where(u => u.UserId == user.Id)
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


    }
}
