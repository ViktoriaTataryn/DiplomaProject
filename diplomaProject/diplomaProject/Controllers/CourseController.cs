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
        // Updated logic to support DTO and multiple questions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(int LessonId, List<QuestionDTO> Questions)
        {
            if (Questions == null || !Questions.Any())
            {
                return BadRequest("Тести не заповнені.");
            }

            var homework = await _context.Homeworks.FirstOrDefaultAsync(h => h.LessonId == LessonId);

            // Create homework automatically if it doesn't exist
            if (homework == null)
            {
                homework = new Homework
                {
                    LessonId = LessonId,
                    Description = "Тест до лекції",
                    DueDate = DateTime.Now.AddDays(7)
                };
                _context.Homeworks.Add(homework);
                await _context.SaveChangesAsync();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var qDto in Questions)
                {
                    var question = new Question
                    {
                        Text = qDto.Text,
                        HomeworkId = homework.Id,
                        IsMultipleChoice = false
                    };
                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();

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

        // --- ІСНУЮЧА ЛОГІКА ---

        // 1. Список всех доступных модулей и уроков
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalModules = await _context.Modules.CountAsync();
            var completedModules = await _context.UserProgresses
                .Where(up => up.UserId == userId && up.Status == ProgressStatus.Completed && up.ModuleId != 0)
                .Select(up => up.ModuleId)
                .Distinct()
                .CountAsync();

            var modulesWithProgress = await _context.Modules
                .Include(m => m.Lessons)
                .OrderBy(m => m.OrderIndex)
                .Select(m => new ModuleProgressDTO
                {
                    ModuleId = m.Id,
                    ModuleNumber = m.OrderIndex,
                    Name = m.Title,
                    ImageForUser = m.ImageUrl,
                    Description = m.Description,
                    TotalModule = totalModules,
                    CompletedModule = completedModules,
                    Status = _context.UserProgresses
                        .Where(up => up.ModuleId == m.Id && up.UserId == userId && up.ModuleId != 0)
                        .Select(up => up.Status.ToString())
                        .FirstOrDefault() ?? "Close",

                    // Old logic restored: Calculating actual percentage
                    Percent = m.Lessons.Any()
                        ? (int)((double)_context.UserProgresses
                            .Count(up => up.UserId == userId && up.ModuleId == m.Id && up.Status == ProgressStatus.Completed && up.LessonId != 0)
                            / m.Lessons.Count * 100)
                        : 0,

                    TotalLesson = m.Lessons.Count,
                    CurrentLessonId = _context.UserProgresses
                        .Where(ulp => ulp.UserId == userId && ulp.ModuleId == m.Id && ulp.LessonId != 0 && ulp.LessonId != null)
                        .OrderByDescending(ulp => ulp.Status == ProgressStatus.InProgress)
                        .ThenByDescending(ulp => ulp.Status == ProgressStatus.Open)
                        .Select(ulp => (int?)ulp.LessonId)
                        .FirstOrDefault(),
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
                .ToListAsync();

            return View(modulesWithProgress);
        }

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

            // --- ПРАВИЛЬНА ЛОГІКА ТЕСТІВ ---
            var homework = await _context.Homeworks
                .FirstOrDefaultAsync(h => h.LessonId == id);

            if (homework != null)
            {
                ViewBag.HomeworkId = homework.Id;

                // Перевіряємо, чи студент вже здав цей тест
                ViewBag.IsTestCompleted = await _context.HomeworkSubmissions
                    .AnyAsync(s => s.HomeworkId == homework.Id && s.StudentId == userId);

                // Отримуємо питання
                ViewBag.Questions = await _context.Questions
                    .Include(q => q.Options)
                    .Where(q => q.HomeworkId == homework.Id)
                    .ToListAsync();

                // Якщо тест пройдено, можна також отримати оцінку (опціонально)
                if (ViewBag.IsTestCompleted)
                {
                    var submission = await _context.HomeworkSubmissions
                        .FirstOrDefaultAsync(s => s.HomeworkId == homework.Id && s.StudentId == userId);
                    ViewBag.CurrentGrade = submission?.Grade ?? 0;
                }
            }
            else
            {
                // Якщо тесту немає — зануляємо все, щоб View не ламався
                ViewBag.HomeworkId = 0;
                ViewBag.IsTestCompleted = false;
                ViewBag.Questions = new List<Question>();
                ViewBag.CurrentGrade = 0;
            }

            // --- SMART NEXT LESSON LOGIC (Cross-module support) ---
            // First check in current module
            var nextLessonId = lesson.Module.Lessons
                .OrderBy(l => l.LessonIndex)
                .FirstOrDefault(l => l.LessonIndex > lesson.LessonIndex)?.Id;

            // If not found, look for the first lesson of the next module
            if (nextLessonId == null)
            {
                var nextModule = await _context.Modules
                    .OrderBy(m => m.OrderIndex)
                    .FirstOrDefaultAsync(m => m.OrderIndex > lesson.Module.OrderIndex);

                if (nextModule != null)
                {
                    nextLessonId = await _context.Lessons
                        .Where(l => l.ModuleId == nextModule.Id)
                        .OrderBy(l => l.LessonIndex)
                        .Select(l => l.Id)
                        .FirstOrDefaultAsync();
                }
            }
            ViewBag.NextLessonId = nextLessonId;

            ViewBag.ModuleProgress = await _context.UserProgresses
                .Where(p => p.UserId == userId && p.ModuleId == lesson.ModuleId)
                .ToListAsync();

            bool isCompleted = await _context.HomeworkSubmissions
                .AnyAsync(s => s.StudentId == userId && s.Homework.LessonId == id);

            ViewBag.IsTestCompleted = isCompleted || TempData["IsTestJustFinished"] != null;

            if (string.IsNullOrEmpty(lesson.Content))
            {
                lesson.Content = "{\"blocks\":[]}";
            }

            return View(lesson);
        }

        // 3. Частичное представление ресурсов
        [HttpGet]
        public async Task<IActionResult> GetResources(int lessonId)
        {
            var resources = await _context.Resources
                .Where(r => r.LessonId == lessonId)
                .ToListAsync();
            return PartialView("_ResourcesPartial", resources);
        }

        // Archive page logic
        [HttpGet]
        public async Task<IActionResult> AdditionalMaterials()
        {
            var modules = await _context.Modules
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Resources)
                .OrderBy(m => m.OrderIndex)
                .ToListAsync();

            return View(modules);
        }

        // 4. Отправка домашнего задания (File based)
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

            var userId = _userManager.GetUserId(User);
            bool alreadySubmitted = await _context.HomeworkSubmissions
                .AnyAsync(s => s.HomeworkId == homeworkId && s.StudentId == userId);

            if (alreadySubmitted)
            {
                return BadRequest("Ви вже здали цей тест.");
            }

            var questions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.HomeworkId == homeworkId)
                .ToListAsync();

            int score = 0;
            int maxScore = questions.Count;
            var selectedOptionIds = answers.Values.SelectMany(x => x).ToList();

            foreach (var question in questions)
            {
                var correctOptionIds = question.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
                var studentIdsForThisQuestion = selectedOptionIds.Intersect(question.Options.Select(o => o.Id)).ToList();

                bool isAnswerPerfect = correctOptionIds.Count == studentIdsForThisQuestion.Count &&
                                       !correctOptionIds.Except(studentIdsForThisQuestion).Any();

                if (isAnswerPerfect) score++;
            }

            var submission = new HomeworkSubmission
            {
                HomeworkId = homeworkId,
                StudentId = userId,
                SubmissionDate = DateTime.Now,
                FilePath = "Quiz Result",
                Status = HomeworkStatus.Approved,
                Grade = score
            };

            foreach (var answer in answers)
            {
                foreach (var optionId in answer.Value)
                {
                    submission.StudentAnswers.Add(new StudentAnswer
                    {
                        QuestionId = answer.Key,
                        SelectedOptionId = optionId
                    });
                }
            }

            _context.HomeworkSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            // Unlock next lesson after successful quiz
            await _progressService.UnlockNextLessonAsync(userId, lessonId);

            TempData["HomeworkResult"] = $"Your score: {score} out of {maxScore}";
            TempData["IsTestJustFinished"] = true;
            TempData["TestJustFinished"] = true;

            return RedirectToAction("Lesson", new { id = lessonId });
        }

        // 6-12. Admin methods & Downloads (Keep original logic)
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

        [HttpGet]
        public async Task<IActionResult> DownloadResource(int resourceId)
        {
            var resource = await _context.Resources.FindAsync(resourceId);
            if (resource == null || string.IsNullOrEmpty(resource.FilePath)) return NotFound();

            if (resource.FilePath.StartsWith("http")) return Redirect(resource.FilePath);

            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials", resource.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(localPath))
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(localPath);
                return File(fileBytes, "application/octet-stream", resource.FileName);
            }
            return NotFound();
        }

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
                            var entryName = $"Module_{resource.Lesson.Module.OrderIndex}/Lesson_{resource.Lesson.LessonIndex}/{resource.FileName}{Path.GetExtension(filePath)}";
                            archive.CreateEntryFromFile(filePath, entryName);
                        }
                    }
                }
                return File(memoryStream.ToArray(), "application/zip", "Full_Course_Archive.zip");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHomework(int homeworkId, string description, DateTime dueDate)
        {
            var homework = await _context.Homeworks.FindAsync(homeworkId);
            if (homework == null) return NotFound();

            homework.Description = description;
            homework.DueDate = dueDate;

            _context.Update(homework);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Homework updated successfully.";
            return RedirectToAction("Lesson", new { id = homework.LessonId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null) return NotFound();

            var homework = await _context.Homeworks.FindAsync(question.HomeworkId);
            int lessonId = homework?.LessonId ?? 0;

            _context.AnswerOptions.RemoveRange(question.Options);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Question deleted successfully.";
            return RedirectToAction("Lesson", new { id = lessonId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubmission(int submissionId)
        {
            var submission = await _context.HomeworkSubmissions.FindAsync(submissionId);
            if (submission == null) return NotFound();

            var homework = await _context.Homeworks.FindAsync(submission.HomeworkId);
            int lessonId = homework?.LessonId ?? 0;

            _context.HomeworkSubmissions.Remove(submission);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Submission removed. Retake is now possible.";
            return RedirectToAction("Lesson", new { id = lessonId });
        }

        [HttpGet]
        public async Task<IActionResult> ViewSubmission(int id)
        {
            var submission = await _context.HomeworkSubmissions
                .Include(s => s.Homework)
                    .ThenInclude(h => h.Questions)
                        .ThenInclude(q => q.Options)
                .Include(s => s.StudentAnswers)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null) return NotFound();

            ViewBag.LessonTitle = submission.Homework?.Lesson?.Title;
            return View(submission);
        }

        [HttpGet]
        public async Task<IActionResult> GetHomeworks()
        {
            var userId = _userManager.GetUserId(User);
            var stats = await _dashboardService.GetHomeworkStats(userId);

            var executedHomeworks = await _context.HomeworkSubmissions
                .Include(s => s.Homework)
                    .ThenInclude(h => h.Lesson)
                        .ThenInclude(l => l.Module)
                .Where(s => s.StudentId == userId)
                .OrderByDescending(s => s.SubmissionDate)
                .ToListAsync();

            //int courseId = 3;
            var course = await _context.Courses.FirstOrDefaultAsync();
            int courseId = course?.Id ?? 0;

            if (courseId == 0)
            {
                return View(new List<Module>()); // Если курсов вообще нет, отдаем пустой список
            }
            var activeLesson = await _progressService.GetActiveLessonAsync(userId, courseId);

            ViewBag.DebugLessonId = activeLesson?.Id.ToString() ?? "null";

            var modules = await _context.Modules.OrderBy(m => m.OrderIndex).ToListAsync();

            Homework currentHomework = null;
            if (activeLesson != null)
            {
                currentHomework = await _context.Homeworks
                    .Include(h => h.Questions)
                    .Include(h => h.Lesson)
                    .FirstOrDefaultAsync(h => h.LessonId == activeLesson.Id);
            }

            var viewModel = new HomeworkDashboardDTO
            {
                Stats = stats ?? new HomeworkStatsDTO(),
                CurrentHomework = currentHomework,
                ExecutedHomeworks = executedHomeworks,
                AllModules = modules,
                AverageScore = executedHomeworks.Any()
                    ? executedHomeworks.Average(s => (double)s.Grade)
                    : 0.0
            };

            return View(viewModel);
        }
    }
}