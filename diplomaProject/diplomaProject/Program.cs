using diplomaProject.Data;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using diplomaProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject
{
    public class Program
    {
        public static void Main(string[] args)
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

            builder.Services.AddScoped<IProgressService, ProgressService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                // Список ролей, які потрібні вашому проекту
                string[] roles = { "Admin", "Student" };

                foreach (var role in roles)
                {
                    if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                    {
                        roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                    }
                }
            }

          

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

           

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); // Хто цей користувач? (Перевірка логіна/пароля)
            app.UseAuthorization();  // Що йому дозволено? (Перевірка ролей)

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
