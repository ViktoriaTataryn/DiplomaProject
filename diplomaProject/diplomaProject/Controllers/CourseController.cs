using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace diplomaProject.Controllers
{
    // [Authorize] - Сюда пускаем только тех, кто залогинился 
    //[Authorize]
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Конструктор: получает доступ к базе данных и менеджеру пользователей
        public CourseController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Course
        // Главная страница курса: показывает список модулей
        public async Task<IActionResult> Index()
        {
            // Берем модули из базы и сразу подгружаем связанные с ними уроки
            var modules = await _context.Modules
                .Include(m => m.Lessons)
                .ToListAsync();

            return View(modules);
        }
    }
}