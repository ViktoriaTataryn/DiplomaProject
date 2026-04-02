using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using diplomaProject.Interfaces;

namespace diplomaProject.Controllers
{
    [Authorize] // Только вошедшие пользователи увидят уроки
    public class CourseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProgressService _progressService;

        public CourseController(AppDbContext context, UserManager<ApplicationUser> userManager, IProgressService progressService)
        {
            _context = context;
            _userManager = userManager;
            _progressService = progressService;
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
            var userId = _userManager.GetUserId(User);

            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            var progress = await _context.UserProgresses
                .FirstOrDefaultAsync(p => p.LessonId == id && p.UserId == userId);

            if (progress != null && progress.Status == ProgressStatus.Open)
            {
                await _progressService.LessonInProgressAsync(userId,lesson.Id);
            } 
            if  (progress == null || progress.Status == ProgressStatus.Close)
            {
                TempData["Error"] = "лекція не доступна. Пройдіть попередні матеріали.";
                return RedirectToAction("Index");
            }

            // Fetch questions and options for this lesson and send them to the View
            ViewBag.Questions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.LessonId == id)
                .ToListAsync();

            return View(lesson);
        }

        // 3. Метод для ресурсов (как просила Вика)
        [HttpGet]
        public async Task<IActionResult> GetResources(int lessonId)
        {
            var resources = await _context.Resources
                .Where(r => r.LessonId == lessonId)
                .ToListAsync();
            return PartialView("_ResourcesPartial", resources);
        }

        // 4. Отправка текста домашнего задания в базу данных и файловую систему
        [HttpPost]
        public async Task<IActionResult> SubmitHomework(int homeworkId, string homeworkText)
        {
            if (string.IsNullOrWhiteSpace(homeworkText))
            {
                ModelState.AddModelError("", "Homework text cannot be empty.");
                return RedirectToAction("Lesson", new { id = homeworkId });
            }

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "submissions");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = $"homework_{homeworkId}_{Guid.NewGuid()}.txt";
            var fullPath = Path.Combine(folderPath, fileName);

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
            return RedirectToAction("Lesson", new { id = homeworkId });
        }

        // 5. Автоматическая проверка и подтверждение домашних заданий
        [HttpPost]
        public async Task<IActionResult> CheckHomework(int lessonId, int homeworkId, List<int> selectedOptionIds)
        {
            var questions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.LessonId == lessonId)
                .ToListAsync();

            int score = 0;
            int maxScore = questions.Count;

            foreach (var question in questions)
            {
                var correctOptionIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id)
                    .ToList();

                var studentIdsForThisQuestion = selectedOptionIds
                    .Intersect(question.Options.Select(o => o.Id))
                    .ToList();

                bool isAnswerPerfect = !correctOptionIds.Except(studentIdsForThisQuestion).Any() &&
                                       !studentIdsForThisQuestion.Except(correctOptionIds).Any();

                if (isAnswerPerfect)
                {
                    score++;
                }
            }

            var userId = _userManager.GetUserId(User);
            var submission = new HomeworkSubmission
            {
                HomeworkId = homeworkId,
                StudentId = userId,
                SubmissionDate = DateTime.Now,
                Status = HomeworkStatus.Approved,
                Grade = score
            };

            _context.HomeworkSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            TempData["HomeworkResult"] = $"Your score: {score} out of {maxScore}";

            return RedirectToAction("Lesson", new { id = lessonId });
        }

        // 6.Способ добавления домашнего задания (для администрации/учителя)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddHomework(Homework model)
        {
            if (ModelState.IsValid)
            {
                _context.Homeworks.Add(model);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Homework added successfully!";
            }
            return RedirectToAction("Lesson", new { id = model.LessonId });
        }

        // 7. Отдельная страница для выполненных домашних заданий
        [HttpGet]
        public async Task<IActionResult> CompletedHomeworks()
        {
            var userId = _userManager.GetUserId(User);

            var completedTasks = await _context.HomeworkSubmissions
                .Include(s => s.Homework)
                .Where(s => s.StudentId == userId && s.Status == HomeworkStatus.Approved)
                .ToListAsync();

            return View(completedTasks);
        }
    }
}