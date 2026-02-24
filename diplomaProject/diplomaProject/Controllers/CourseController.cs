using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace diplomaProject.Controllers
{
    // [Authorize] - Сюда пускаем только тех, кто залогинился 
    // Пока закомментировано, так как у Вики нет View для логина
    // [Authorize]
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

        // GET: Course/Lesson/5
        // Страница конкретного урока
        public async Task<IActionResult> Lesson(int id)
        {
            // Ищем урок в базе по его ID
            // Include(l => l.Resources) нужен, чтобы подтянуть доп. материалы (файлы к уроку)
            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == id);

            // Если вдруг урока с таким номером нет (кто-то ввел ID вручную)
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        // GET: Course/SubmitHomework/5
        public IActionResult SubmitHomework(int id)
        {
            // Передаем ID урока, чтобы знать, к чему привязана домашка
            ViewBag.LessonId = id;
            return View();
        }
    }
}