using diplomaProject.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace diplomaProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    // Если зашел админ сразу на список модулей
                    return RedirectToAction("GetModules", "Admin");
                }
                else
                {
                    // Если студент на главную страницу курсов
                    return RedirectToAction("Index", "Course");
                }
            }
            // Если не залогинен — показываем обычный лендинг или страницу логина
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
    }
}
