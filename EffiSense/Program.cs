using EffiSense.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EffiSense
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Добави услуги към контейнера.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                // Конфигуриране на Lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Време за блокиране (например 5 минути)
                options.Lockout.MaxFailedAccessAttempts = 5; // Максимален брой неуспешни опити за логин
                options.Lockout.AllowedForNewUsers = true; // Разрешава lockout за нови потребители
            })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            await InitializeRoles(app.Services);

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }

        public static async Task InitializeRoles(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Проверка дали ролята "Administrator" съществува
                if (!await roleManager.RoleExistsAsync("Administrator"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Administrator"));
                    Console.WriteLine("Administrator role created.");
                }

                var adminEmail = "Stanimir@gmail.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    // Създаване на нов администраторски потребител
                    var newAdmin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
                    var result = await userManager.CreateAsync(newAdmin, "SecurePassword123!");

                    if (result.Succeeded)
                    {
                        Console.WriteLine("Admin created successfully.");

                        // Добавяне на роля "Administrator" към новия администратор
                        var roleResult = await userManager.AddToRoleAsync(newAdmin, "Administrator");
                        if (roleResult.Succeeded)
                        {
                            Console.WriteLine("Admin added to Administrator role.");
                        }
                        else
                        {
                            foreach (var error in roleResult.Errors)
                            {
                                Console.WriteLine($"Error adding admin to role: {error.Description}");
                            }
                        }

                        // Разрешаване на lockout (ако е необходимо)
                        await userManager.SetLockoutEnabledAsync(newAdmin, true); // Активиране на lockout
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error: {error.Description}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Admin user already exists.");
                }
            }
        }
    }
}
