using System.Diagnostics;
using diplomaProject.Models;
using Microsoft.AspNetCore.Identity;

namespace diplomaProject.Data;

public static class DbInitializer
{
    public static async Task SeedAdminUser(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Створення ролей (залишається так само)
        string[] roles = { "Admin", "Student" };
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        var adminEmail = "admin@artcourse.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            // Створюємо саме ApplicationUser
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                // ДОДАЄМО ОБОВ'ЯЗКОВІ ПОЛЯ:
                FirstName = "Admin",
                LastName = "System",
                RegistrationDate = DateTime.Now
            };

            var result = await userManager.CreateAsync(user, "admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                Debug.WriteLine("АДМІНА СТВОРЕНО УСПІШНО!");
            }
            else
            {
                // Це покаже нам в Output, ЧОМУ адмін не створився
                foreach (var error in result.Errors)
                    Debug.WriteLine($"ПОМИЛКА IDENTITY: {error.Code} - {error.Description}");
            }
        }
    }

    public static async Task SeedCourse(AppDbContext context)
    {
        if (!context.Courses.Any())
        {
            var course = new Course
            {
                Title = "Основи програмування",
                Description = "Цей курс допоможе вам освоїти основи програмування на C#."
            };
            context.Courses.Add(course);
            await context.SaveChangesAsync();
        }
    }
}