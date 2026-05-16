using CloudinaryDotNet;
using diplomaProject.Data;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using diplomaProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace diplomaProject
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {

                // Налаштування паролів 
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                // Налаштування унікальності пошти
                options.User.RequireUniqueEmail = true;

                options.SignIn.RequireConfirmedAccount = true;

            })
                .AddEntityFrameworkStores<AppDbContext>() // Де зберігати дані
                .AddDefaultTokenProviders(); // Потрібно для скидання паролів тощо

            builder.Services.AddTransient<IEmailSender, EmailSender>();

            // 2. Налаштування кукі (куди кидати користувача, якщо він не авторизований)
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";  // Шлях до вашої сторінки входу
                options.AccessDeniedPath = "/Auth/AccessDenied"; // Якщо немає прав (наприклад, не Адмін)
            });

            var cloudName = builder.Configuration["CloudinarySettings:CloudName"];
            var apiKey = builder.Configuration["CloudinarySettings:ApiKey"];
            var apiSecret = builder.Configuration["CloudinarySettings:ApiSecret"];

            CloudinaryDotNet.Account account = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
            Cloudinary cloudinary = new Cloudinary(account);

            builder.Services.AddSingleton(cloudinary);

            StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

            // Added HttpClient to support file downloads from Cloudinary
            builder.Services.AddHttpClient();

            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IProgressService, ProgressService>();
            builder.Services.AddScoped<ProgressService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();


            var app = builder.Build();

            //using (var scope = app.Services.CreateScope())
            //{
            //    var services = scope.ServiceProvider;
            //    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            //    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            //    await DbInitializer.SeedAdminUser(userManager, roleManager);

            //    // Список ролей, які потрібні вашому проекту
            //    string[] roles = { "Admin", "Student" };

            //    foreach (var role in roles)
            //    {
            //        if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            //        {
            //            roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            //        }
            //    }
            //}

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                    // Тепер await спрацює без помилок
                    await DbInitializer.SeedAdminUser(userManager, roleManager);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Помилка під час створення початкових даних.");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); // Хто цей користувач? (Перевірка логіна/пароля)
            app.UseAuthorization();  // Що йому дозволено? (Перевірка ролей)

            //app.MapControllerRoute(
            //    name: "default",
            //    pattern: "{controller=User}/{action=Dashboard}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}