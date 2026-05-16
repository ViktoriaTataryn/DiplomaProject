using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace diplomaProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new LandingDTO();

      
            model.Modules = await _context.Modules.Select(m => new ModuleProgressDTO
            {
                ModuleNumber = m.OrderIndex,
                Name = m.Title

            }).ToListAsync();

           
            model.Reviews = await _context.Reviews
                .Include(r => r.User) 
    .OrderByDescending(r => r.Id)
                .Take(20)
                .Select(r => new ReviewDTO
                {
                    Id = r.Id,
                    UserName = r.User.FirstName, 
                    Content = r.Content,
                })
                .ToListAsync();

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
}
