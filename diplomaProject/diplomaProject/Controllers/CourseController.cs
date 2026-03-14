using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace diplomaProject.Controllers
{
    [Authorize] // Только вошедшие пользователи увидят уроки
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Список курсов
        public async Task<IActionResult> Index()
        {
            var modules = await _context.Modules.Include(m => m.Lessons).ToListAsync();
            return View(modules);
        }

        // 2. Страница урока
        public async Task<IActionResult> Lesson(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();
            return View(lesson);
        }

        // 3. Метод для ресурсов (как просила Вика)
        // Позволяет получить список файлов отдельно, если нужно
        [HttpGet]
        public async Task<IActionResult> GetResources(int lessonId)
        {
            var resources = await _context.Resources
                .Where(r => r.LessonId == lessonId)
                .ToListAsync();
            return PartialView("_ResourcesPartial", resources);
        }


        // 4. Отправка домашки через POST (без отдельной вьюшки)
        [HttpPost]
        public async Task<IActionResult> SubmitHomework(int homeworkId, string homeworkText)
        {
            // Тут в будущем будет логика сохранения в базу через ProgressService Вики
            // Пока просто делаем заглушку, чтобы кнопка работала

            if (string.IsNullOrWhiteSpace(homeworkText))
            {
                ModelState.AddModelError("", "Текст завдання не може бути порожнім.");
                return RedirectToAction( "Lesson", new { id = homeworkId }); // Повертаємо на сторінку уроку

            }
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "submissions");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
           
            var fileName = $"homework_{homeworkId}_{Guid.NewGuid()}.txt";
            var fullPath = Path.Combine(folderPath, fileName);

            //  Записуємо текст у файл
            await System.IO.File.WriteAllTextAsync(fullPath, homeworkText);

            var userId = _userManager.GetUserId(User);

            var submission = new HomeworkSubmission
            {
                HomeworkId = homeworkId,
                StudentId = userId,
                FilePath = "/submissions/" + fileName,
                SubmissionDate = DateTime.Now,
                Status = HomeworkStatus.Pending
            };

            _context.HomeworkSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Homework submitted successfully!";
            return RedirectToAction("Lesson");
        }
    }
}