using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using diplomaProject.Services;
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
        public readonly IDashboardService _dashboardService;


        public CourseController(AppDbContext context, UserManager<ApplicationUser> userManager, IProgressService progressService, IDashboardService dashboardService)
        {
            _context = context;
            _userManager = userManager;
            _progressService = progressService;
            _dashboardService = dashboardService;
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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AddQuestion(Question question, List<string> options, int correctOptionIndex)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // 1. Додаємо питання в БД
        //        _context.Questions.Add(question);
        //        await _context.SaveChangesAsync();

        //        // 2. Додаємо варіанти відповідей
        //        for (int i = 0; i < options.Count; i++)
        //        {
        //            var option = new AnswerOption
        //            {
        //                Text = options[i],
        //                IsCorrect = (i == correctOptionIndex),
        //                QuestionId = question.Id
        //            };
        //            _context.AnswerOptions.Add(option);
        //        }

        //        await _context.SaveChangesAsync();

        //        return RedirectToAction("Lesson", new { id = question.LessonId });
        //    }

        //    ViewBag.LectureId = question.LessonId;
        //    return View(question);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int LessonId, List<QuestionDTO> Questions)
        {
            // Перевіряємо, чи є питання у списку
            if (Questions == null || !Questions.Any())
            {
                return BadRequest("Тести не заповнені.");
            }
            var homework = await _context.Homeworks.FirstOrDefaultAsync(h => h.LessonId == LessonId);

            // Якщо домашки ще немає — створимо її автоматично (бо питання мають до чогось кріпитися)
            if (homework == null)
            {
                homework = new Homework
                {
                    LessonId = LessonId,
                    Description = "Тест до лекції", // Базовий опис
                    DueDate = DateTime.Now.AddDays(7)
                };
                _context.Homeworks.Add(homework);
                await _context.SaveChangesAsync();
            }

            // Використовуємо транзакцію, щоб якщо одне питання впаде, нічого не збереглося
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var qDto in Questions)
                {
                    // 1. Створюємо об'єкт питання
                    var question = new Question
                    {
                        Text = qDto.Text,
                        HomeworkId = homework.Id, // ВИПРАВЛЕНО: тепер зв'язок через домашку
                        IsMultipleChoice = false
                    };
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync(); // Зберігаємо, щоб отримати Id питання

                    // 2. Додаємо варіанти відповідей для цього питання
                    for (int i = 0; i < qDto.Answers.Count; i++)
                    {
                        var option = new AnswerOption
                        {
                            Text = qDto.Answers[i].Text,
                            IsCorrect = (i == qDto.CorrectAnswerIndex),
                            QuestionId = question.Id
                        };
                        _context.AnswerOptions.Add(option);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                //return RedirectToAction("Lesson", new { id = LessonId });
                return RedirectToAction("GetLessons", "AdminLesson");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Помилка при збереженні тестів: " + ex.Message);
                ViewBag.LectureId = LessonId;
                return View(Questions);
            }
        }

        // --- ІСНУЮЧА ЛОГІКА (БЕЗ ЗМІН) ---

        // 1. Список всех доступных модулей и уроков
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalModules = _context.Modules.Count();
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
                    Description = m.Description,
                    TotalModule = totalModules,
                    CompletedModule = completedModules,

                    // Дістаємо дані з таблиці UserProgress для цього модуля і користувача
                    // Якщо запису немає, ставимо статус "Close"
                    Status = _context.UserProgresses
                        .Where(up => up.ModuleId == m.Id && up.UserId == userId && up.ModuleId != 0)
                        .Select(up => up.Status.ToString())
                        .FirstOrDefault() ?? "Close",
                    Percent = 0,
                    TotalLesson = m.Lessons.Count,
                    //        CurrentLessonId = _context.UserProgresses
                    //.Where(ulp => ulp.UserId == userId && ulp.ModuleId == m.Id && ulp.LessonId != 0)
                    //.OrderByDescending(ulp => ulp.Status == ProgressStatus.InProgress)
                    //.ThenByDescending(ulp => ulp.Status == ProgressStatus.Open)
                    //.ThenBy(ulp => ulp.Lesson.LessonIndex)
                    //.Select(ulp => ulp.LessonId)
                    //.FirstOrDefault(),
                    CurrentLessonId = _context.UserProgresses
    .Where(ulp => ulp.UserId == userId && ulp.ModuleId == m.Id && ulp.LessonId != 0 && ulp.LessonId != null)
    .OrderByDescending(ulp => ulp.Status == ProgressStatus.InProgress)
    .ThenByDescending(ulp => ulp.Status == ProgressStatus.Open)
    .Select(ulp => (int?)ulp.LessonId) // Примусово кастимо до nullable
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
        //public async Task<IActionResult> Index()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    int courseId = 3; // Або отримайте актуальний ID курсу

        //    // Викликаємо ваш сервіс, де ми вже налагодили логіку
        //    var dashboardData = await _dashboardService.GetUserStatistic(userId, courseId);

        //    // Передаємо у View саме список модулів
        //    return View(dashboardData.ModuleProgress);
        //}

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
            var homework = await _context.Homeworks
        .FirstOrDefaultAsync(h => h.LessonId == id);
            if (homework != null)
            {
                // 2. Передаємо ID домашки, щоб спрацював CheckHomework
                //  ViewBag.HomeworkId = homework.Id;
                //  ViewBag.Questions = await _context.Questions
                //.Include(q => q.Options)
                //.Where(q => q.HomeworkId == homework.Id)
                //.ToListAsync();
                ViewBag.HomeworkId = homework.Id;
                ViewBag.IsTestCompleted = await _context.HomeworkSubmissions
                    .AnyAsync(s => s.HomeworkId == homework.Id && s.StudentId == userId);

                ViewBag.Questions = await _context.Questions
                    .Include(q => q.Options)
                    .Where(q => q.HomeworkId == homework.Id)
                    .ToListAsync();



            }
            else
            {
                ViewBag.HomeworkId = 0;
                ViewBag.IsTestCompleted = false;
                ViewBag.Questions = new List<Question>();
            }

            ViewBag.ModuleProgress = await _context.UserProgresses
             .Where(p => p.UserId == userId && p.ModuleId == lesson.ModuleId)
             .ToListAsync();

            var isTestCompleted = await _context.HomeworkSubmissions
    .AnyAsync(s => s.HomeworkId == homework.Id && s.StudentId == userId);

            ViewBag.IsTestCompleted = isTestCompleted;

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

        // NEW: Full page for Additional Materials (Archive)
        [HttpGet]
        public async Task<IActionResult> AdditionalMaterials()
        {
            // Fetching all modules with their lessons and resources to display the archive page
            var modules = await _context.Modules
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Resources)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync();

            return View(modules);
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
        public async Task<IActionResult> CheckHomework(int lessonId, int homeworkId, Dictionary<int, List<int>> answers)
        {
            if (answers == null || !answers.Any())
            {
                TempData["Error"] = "Будь ласка, оберіть хоча б одну відповідь.";
                return RedirectToAction("Lesson", new { id = lessonId });
            }
            var selectedOptionIds = answers.Values.SelectMany(x => x).ToList();
            //var questions = await _context.Questions
            //    .Include(q => q.Options)
            //    .Where(q => q.LessonId == lessonId)
            //    .ToListAsync();
            var questions = await _context.Questions
        .Include(q => q.Options)
        .Where(q => q.HomeworkId == homeworkId) // Тепер фільтруємо по HomeworkId
        .ToListAsync();
            if (!questions.Any())
            {
                var homework = await _context.Homeworks
                   .Include(h => h.Questions)
                   .ThenInclude(q => q.Options)
                   .FirstOrDefaultAsync(h => h.LessonId == lessonId);

                if (homework != null)
                {
                    questions = homework.Questions.ToList();
                    homeworkId = homework.Id; // Оновлюємо, якщо прийшов 0
                }
            }

            int score = 0;
            int maxScore = questions.Count;

            //foreach (var question in questions)
            //{
            //    var correctOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
            //    var studentIdsForThisQuestion = selectedOptionIds.Intersect(question.Options.Select(o => o.Id)).ToList();

            //    bool isAnswerPerfect = !correctOptionIds.Except(studentIdsForThisQuestion).Any() &&
            //                           !studentIdsForThisQuestion.Except(correctOptionIds).Any();

            //    if (isAnswerPerfect) score++;
            //}
            foreach (var question in questions)
            {
                var correctOptionIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id)
                    .ToList();

                var studentIdsForThisQuestion = selectedOptionIds
                    .Intersect(question.Options.Select(o => o.Id))
                    .ToList();

                // Перевірка на повну відповідність (ідеальна відповідь)
                bool isAnswerPerfect = correctOptionIds.Count == studentIdsForThisQuestion.Count &&
                                       !correctOptionIds.Except(studentIdsForThisQuestion).Any() &&
                                       !studentIdsForThisQuestion.Except(correctOptionIds).Any();

                if (isAnswerPerfect) score++;
            }

            var userId = _userManager.GetUserId(User);
            bool alreadySubmitted = await _context.HomeworkSubmissions
        .AnyAsync(s => s.HomeworkId == homeworkId && s.StudentId == userId);

            if (alreadySubmitted)
            {
                return BadRequest("Ви вже здали цей тест.");
            }
            // 1. Створюємо об'єкт здачі
            var submission = new HomeworkSubmission
            {
                HomeworkId = homeworkId,
                StudentId = userId,
                SubmissionDate = DateTime.Now,
                FilePath = "Quiz Result",
                Status = HomeworkStatus.Approved,
                Grade = score
            };

            // 2. Додаємо відповіді студента до об'єкта submission
            if (answers != null)
            {
                foreach (var answer in answers)
                {
                    var questionId = answer.Key;
                    foreach (var optionId in answer.Value)
                    {
                        submission.StudentAnswers.Add(new StudentAnswer
                        {
                            QuestionId = questionId,
                            SelectedOptionId = optionId
                            // HomeworkSubmissionId заповниться автоматично завдяки EF Core!
                        });
                    }
                }
            }

            _context.HomeworkSubmissions.Add(submission);
            await _context.SaveChangesAsync();
            //var submission = new HomeworkSubmission
            //{
            //    HomeworkId = homeworkId,
            //    StudentId = userId,
            //    SubmissionDate = DateTime.Now,
            //    FilePath="test quiz",
            //    Status = HomeworkStatus.Approved,

            //    Grade = score
            //};

            //_context.HomeworkSubmissions.Add(submission);
            //await _context.SaveChangesAsync();

            await _progressService.UnlockNextLessonAsync(userId, lessonId);



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

            // If it's a Cloudinary/External link
            if (resource.FilePath.StartsWith("http")) return Redirect(resource.FilePath);

            // If it's a local file
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials", resource.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(localPath))
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(localPath);
                return File(fileBytes, "application/octet-stream", resource.FileName);
            }

            return NotFound();
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
        // NEW: Download the entire course as one ZIP archive
        [HttpGet]
        public async Task<IActionResult> DownloadFullArchive()
        {
            var resources = await _context.Resources
                .Include(r => r.Lesson)
                    .ThenInclude(l => l.Module)
                .ToListAsync();

            if (!resources.Any()) return BadRequest("No materials found in the course.");

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var resource in resources)
                    {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials", resource.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            // Organizing by Module/Lesson/FileName inside ZIP
                            var entryName = $"Module_{resource.Lesson.Module.OrderIndex}/Lesson_{resource.Lesson.LessonIndex}/{resource.FileName}{Path.GetExtension(filePath)}";
                            archive.CreateEntryFromFile(filePath, entryName);
                        }
                    }
                }
                return File(memoryStream.ToArray(), "application/zip", "Full_Course_Archive.zip");
            }
        }
        // 10. Обновление информации о домашнем задании, включая описание и срок выполнения (только для администратора)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHomework(int homeworkId, string description, DateTime dueDate)
        {
            var homework = await _context.Homeworks.FindAsync(homeworkId);
            if (homework == null)
            {
                return NotFound();
            }

            homework.Description = description;
            homework.DueDate = dueDate;

            _context.Update(homework);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Homework updated successfully.";
            return RedirectToAction("Lesson", new { id = homework.LessonId });
        }

        // 11. Удаление конкретного вопроса из теста (только для администратора)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
            {
                return NotFound();
            }

            var homework = await _context.Homeworks.FindAsync(question.HomeworkId);
            int lessonId = homework?.LessonId ?? 0;

            _context.AnswerOptions.RemoveRange(question.Options);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Question deleted successfully.";
            return RedirectToAction("Lesson", new { id = lessonId });
        }
        // 12. Delete a specific submission to allow a student to retake the test (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubmission(int submissionId)
        {
            var submission = await _context.HomeworkSubmissions.FindAsync(submissionId);
            if (submission == null)
            {
                return NotFound();
            }

            var homework = await _context.Homeworks.FindAsync(submission.HomeworkId);
            int lessonId = homework?.LessonId ?? 0;

            _context.HomeworkSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Submission removed. Retake is now possible.";
            return RedirectToAction("Lesson", new { id = lessonId });
        }


        [HttpGet]
        public async Task<IActionResult> ViewSubmission(int id) // id — це Id з таблиці HomeworkSubmissions
        {
            var submission = await _context.HomeworkSubmissions
                .Include(s => s.Homework)
                    .ThenInclude(h => h.Questions)
                        .ThenInclude(q => q.Options)
                .Include(s => s.StudentAnswers) // Завантажуємо відповіді, які зберіг студент
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
            {
                return NotFound();
            }

           
            ViewBag.LessonTitle = submission.Homework?.Lesson?.Title;

            return View(submission);
        }

        [HttpGet]
        public async Task<IActionResult> GetHomeworks()
        {
            var userId = _userManager.GetUserId(User);

            // 1. Отримуємо статистику
            var stats = await _dashboardService.GetHomeworkStats(userId);

            // 2. Отримуємо список виконаних робіт (Виправлено Include)
            var executedHomeworks = await _context.HomeworkSubmissions
                .Include(s => s.Homework)
                    .ThenInclude(h => h.Lesson)
                        .ThenInclude(l => l.Module)
                .Where(s => s.StudentId == userId)
                .OrderByDescending(s => s.SubmissionDate)
                .ToListAsync();

            // 3. Отримуємо поточне завдання
            
            int courseId = 3; 

            // 1. Отримуємо активну лекцію (тут уже працює наша виправлена логіка з HasValue)
            var activeLesson = await _progressService.GetActiveLessonAsync(userId, courseId);

            // Для дебагу на сторінці
            ViewBag.DebugLessonId = activeLesson?.Id.ToString() ?? "null";

            Homework currentHomework = null;
            if (activeLesson != null)
            {
                // 2. Шукаємо домашку саме для цієї лекції
                currentHomework = await _context.Homeworks
                    .Include(h => h.Questions)
                    .Include(h => h.Lesson)
                    .FirstOrDefaultAsync(h => h.LessonId == activeLesson.Id);
            }

            ViewBag.HomeworkFound = currentHomework != null ? "Yes" : "No";

            var viewModel = new HomeworkDashboardDTO
            {
                Stats = stats ?? new HomeworkStatsDTO(),
                CurrentHomework = currentHomework,
                ExecutedHomeworks = executedHomeworks,
                // Виправлено Average
                AverageScore = executedHomeworks.Any()
                    ? executedHomeworks.Average(s => (double)s.Grade)
                    : 0.0
            };

            return View(viewModel);
        }
    }
}