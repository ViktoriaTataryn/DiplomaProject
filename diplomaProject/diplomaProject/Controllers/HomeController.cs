using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
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

        [AllowAnonymous]
        public IActionResult Index()
        {
            // 1. Проверяем, залогинен ли пользователь
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // 2. Если это Админ, отправляем в управление модулями
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("GetModules", "Admin");
                }
                // 3. Если это студент тогда отправляем на его дашборд
                else if (User.IsInRole("Student"))
                {
                    return RedirectToAction("Dashboard", "User");
                }
            }

            // 4. Если гость (Вика еще не пушнула лендинг) то показываем стандартную главную
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}   