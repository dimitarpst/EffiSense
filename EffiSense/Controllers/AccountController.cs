using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EffiSense.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

       
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                Console.WriteLine("User not found.");
                return Unauthorized();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, false, false);
            if (!result.Succeeded)
            {
                Console.WriteLine("Invalid login attempt.");
                return Unauthorized();
            }

            return RedirectToAction("Index", "Home");
        }

    }

}
