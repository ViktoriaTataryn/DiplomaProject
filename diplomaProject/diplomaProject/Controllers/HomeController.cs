using System.Diagnostics;
using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Controllers;

public class HomeController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return User.IsInRole("Admin")
                ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
                : RedirectToAction("Dashboard", "User", new { area = "" });

        var model = new LandingDto
        {
            Modules = await context.Modules.Select(m => new ModuleProgressDto
            {
                ModuleNumber = m.OrderIndex,
                Name = m.Title
            }).ToListAsync(),
            Reviews = await context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.Id)
                .Take(20)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.User!.FirstName,
                    Content = r.Content
                })
                .ToListAsync()
        };

        return View(model);
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
}