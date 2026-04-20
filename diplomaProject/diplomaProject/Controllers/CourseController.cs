using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Claims;

namespace diplomaProject.Controllers
{
    [Authorize]
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


        // GET: Course/AddQuestion?lectureId=5
        [HttpGet]
        public IActionResult AddQuestion(int lectureId)
        {
            // Передаємо ID лекції у View, щоб прив'язати питання
            ViewBag.LectureId = lectureId;
            return View();
        }

        // POST: Course/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(Question question, List<string> options, int correctOptionIndex)
        {
            if (ModelState.IsValid)
            {
                // 1. Додаємо питання в БД
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                // 2. Додаємо варіанти відповідей
                for (int i = 0; i < options.Count; i++)
                {
                    var option = new AnswerOption
                    {
                        Text = options[i],
                        IsCorrect = (i == correctOptionIndex),
                        QuestionId = question.Id
                    };
                    _context.AnswerOptions.Add(option);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Lesson", new { id = question.LessonId });
            }

            ViewBag.LectureId = question.LessonId;
            return View(question);
        }

        // --- ІСНУЮЧА ЛОГІКА (БЕЗ ЗМІН) ---

        // 1. Список всех доступных модулей и уроков
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalModules=_context.Modules.Count();
            var completedModules = _context.UserProgresses
    .Where(up => up.UserId == userId && up.Status == ProgressStatus.Completed && up.ModuleId != 0)
    .Select(up => up.ModuleId)
    .Distinct() //без дублікатів
    .Count();

            var modulesWithProgress = _context.Modules
                .Include(m => m.Lessons)
                .Select(m => new ModuleProgressDTO
                {
                    ModuleId = m.Id,
                    ModuleNumber = m.OrderIndex,
                    Name = m.Title,
                    ImageForUser = m.ImageUrl,
                    Description=m.Description,
                    TotalModule=totalModules,
                    CompletedModule=completedModules,

                    // Дістаємо дані з таблиці UserProgress для цього модуля і користувача
                    // Якщо запису немає, ставимо статус "Close"
                    Status = _context.UserProgresses
                        .Where(up => up.ModuleId == m.Id && up.UserId == userId && up.ModuleId != 0)
                        .Select(up => up.Status.ToString())
                        .FirstOrDefault() ?? "Close",
                    Percent = 0,
                    TotalLesson = m.Lessons.Count,
                    CurrentLessonId = _context.UserProgresses
            .Where(ulp => ulp.UserId == userId && ulp.ModuleId == m.Id && ulp.LessonId != 0)
            .OrderByDescending(ulp => ulp.Status == ProgressStatus.InProgress)
            .ThenByDescending(ulp => ulp.Status == ProgressStatus.Open)
            .ThenBy(ulp => ulp.Lesson.LessonIndex)
            .Select(ulp => ulp.LessonId)
            .FirstOrDefault(),
                    // Наповнюємо список лекцій для вертикального списку
                    Lessons = m.Lessons.Select(l => new LessonShortDTO
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Status = _context.UserProgresses
                            .Where(ulp => ulp.LessonId == l.Id && ulp.UserId == userId && ulp.LessonId != 0)
                            .Select(ulp => ulp.Status.ToString())
                            .FirstOrDefault() ?? "Close"
                    }).ToList()
                })
                .OrderBy(m => m.ModuleNumber)
                .ToList();

            return View(modulesWithProgress);
        }

        // 2. Главная страница урока с боковой панелью и отслеживанием прогресса
        public async Task<IActionResult> Lesson(int id)
        {
            var userId = _userManager.GetUserId(User);

            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .Include(l => l.Module)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();

            var progress = await _context.UserProgresses
                .FirstOrDefaultAsync(p => p.LessonId == id && p.UserId == userId);

            if (progress == null || progress.Status == ProgressStatus.Close)
            {
                TempData["Error"] = "Lecture is not available. Please complete previous materials.";
                return RedirectToAction("Index");
            }

            if (progress.Status == ProgressStatus.Open)
            {
                await _progressService.LessonInProgressAsync(userId, lesson.Id);
            }

            ViewBag.Questions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.LessonId == id)
                .ToListAsync();

            ViewBag.ModuleProgress = await _context.UserProgresses
                .Where(p => p.UserId == userId && p.ModuleId == lesson.ModuleId)
                .ToListAsync();

            if (string.IsNullOrEmpty(lesson.Content))
            {
                lesson.Content = "{}";
            }

            return View(lesson);
        }

        // 3. Частичное представление для динамической загрузки ресурсов
        [HttpGet]
        public async Task<IActionResult> GetResources(int lessonId)
        {
            var resources = await _context.Resources
                .Where(r => r.LessonId == lessonId)
                .ToListAsync();
            return PartialView("_ResourcesPartial", resources);
        }

        // 4. Отправка текста домашнего задания и сохранение в виде .txt файла
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

        // 5. Автоматическая проверка теста
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
                var correctOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
                var studentIdsForThisQuestion = selectedOptionIds.Intersect(question.Options.Select(o => o.Id)).ToList();

                bool isAnswerPerfect = !correctOptionIds.Except(studentIdsForThisQuestion).Any() &&
                                       !studentIdsForThisQuestion.Except(correctOptionIds).Any();

                if (isAnswerPerfect) score++;
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

        // 6. Метод администратора для добавления нового домашнего задания
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

        // 7. Просмотр истории одобренных отправок
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

        // 8. Загрузка/Перенаправление к ресурсу (поддержка Cloudinary)
        [HttpGet]
        public async Task<IActionResult> DownloadResource(int resourceId)
        {
            var resource = await _context.Resources.FindAsync(resourceId);
            if (resource == null || string.IsNullOrEmpty(resource.FilePath)) return NotFound();
            return Redirect(resource.FilePath);
        }

        // 9. Сжатие всех материалов модуля для массовой загрузки
        [HttpGet]
        public async Task<IActionResult> DownloadModuleMaterials(int moduleId)
        {
            var resources = await _context.Resources
                .Include(r => r.Lesson)
                .Where(r => r.Lesson.ModuleId == moduleId)
                .ToListAsync();

            if (!resources.Any()) return BadRequest("No materials found for this module.");

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var resource in resources)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials", resource.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            archive.CreateEntryFromFile(filePath, resource.FileName + Path.GetExtension(filePath));
                        }
                    }
                }
                return File(memoryStream.ToArray(), "application/zip", $"Module_{moduleId}_Materials.zip");
            }
        }
    }
}