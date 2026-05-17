using System.IO.Compression;
using System.Security.Claims;
using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Added for Cloudinary downloads

namespace diplomaProject.Controllers;

[Authorize(Roles = "Student,Admin")]
public class CourseController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IProgressService progressService,
    IDashboardService dashboardService,
    IHttpClientFactory httpClientFactory)
    : Controller
{
    // --- Action for the Additional Materials page ---
    [HttpGet]
    public async Task<IActionResult> AdditionalMaterials()
    {
        // Fetching all modules with their lessons and associated resources
        var modules = await context.Modules
            .Include(m => m.Lessons)!
            .ThenInclude(l => l.Resources)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync();

        return View(modules);
    }

    // --- Method for downloading a single resource file ---
    [HttpGet]
    public async Task<IActionResult> DownloadResource(int resourceId)
    {
        var resource = await context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        if (resource == null) return NotFound();

        var client = httpClientFactory.CreateClient();
        byte[]? fileBytes = null;

        if (resource.FilePath.StartsWith("http"))
        {
            try
            {
                fileBytes = await client.GetByteArrayAsync(resource.FilePath);
            }
            catch
            {
                return BadRequest("Could not download file from cloud.");
            }
        }
        else
        {
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials",
                resource.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(localPath)) fileBytes = await System.IO.File.ReadAllBytesAsync(localPath);
        }

        if (fileBytes == null) return NotFound("File not found on server.");

        return File(fileBytes, "application/octet-stream", resource.FileName);
    }

    // --- Existing Methods (Index, Lesson, etc.) remain unchanged ---

    [HttpGet]
    public IActionResult AddQuestion(int lectureId)
    {
        ViewBag.LectureId = lectureId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(int lessonId, List<QuestionDto> questions)
    {
        if (questions.Count == 0) return BadRequest("Тести не заповнені.");

        var homework = await context.Homeworks.FirstOrDefaultAsync(h => h.LessonId == lessonId);

        if (homework == null)
        {
            homework = new Homework
            {
                LessonId = lessonId,
                Description = "Тест до лекції",
                DueDate = DateTime.Now.AddDays(7)
            };
            context.Homeworks.Add(homework);
            await context.SaveChangesAsync();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            foreach (var qDto in questions)
            {
                var question = new Question
                {
                    Text = qDto.Text,
                    HomeworkId = homework.Id,
                    IsMultipleChoice = false
                };
                context.Questions.Add(question);
                await context.SaveChangesAsync();

                for (var i = 0; i < qDto.Answers.Count; i++)
                {
                    var option = new AnswerOption
                    {
                        Text = qDto.Answers[i].Text,
                        IsCorrect = i == qDto.CorrectAnswerIndex,
                        QuestionId = question.Id
                    };
                    context.AnswerOptions.Add(option);
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return RedirectToAction("Index", "Home", new { area = "" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError("", "Помилка при збереженні тестів: " + ex.Message);
            ViewBag.LectureId = lessonId;
            return View(questions);
        }
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var totalModules = await context.Modules.CountAsync();
        var completedModules = await context.UserProgresses
            .Where(up => up.UserId == userId && up.Status == ProgressStatus.Completed && up.ModuleId != 0)
            .Select(up => up.ModuleId)
            .Distinct()
            .CountAsync();

        var modulesWithProgress = await context.Modules
            .Include(m => m.Lessons)
            .OrderBy(m => m.OrderIndex)
            .Select(m => new ModuleProgressDto
            {
                ModuleId = m.Id,
                ModuleNumber = m.OrderIndex,
                Name = m.Title,
                ImageForUser = m.ImageUrl,
                Description = m.Description,
                TotalModule = totalModules,
                CompletedModule = completedModules,
                Status = context.UserProgresses
                    .Where(up => up.ModuleId == m.Id && up.UserId == userId && up.ModuleId != 0)
                    .Select(up => up.Status.ToString())
                    .FirstOrDefault() ?? "Close",

                Percent = m.Lessons!.Any()
                    ? (int)((double)context.UserProgresses
                            .Count(up =>
                                up.UserId == userId && up.ModuleId == m.Id && up.Status == ProgressStatus.Completed &&
                                up.LessonId != 0)
                        / m.Lessons!.Count * 100)
                    : 0,

                TotalLesson = m.Lessons!.Count,
                CurrentLessonId = context.UserProgresses
                    .Where(ulp =>
                        ulp.UserId == userId && ulp.ModuleId == m.Id && ulp.LessonId != 0 && ulp.LessonId != null)
                    .OrderByDescending(ulp => ulp.Status == ProgressStatus.InProgress)
                    .ThenByDescending(ulp => ulp.Status == ProgressStatus.Open)
                    .Select(ulp => ulp.LessonId)
                    .FirstOrDefault(),
                Lessons = m.Lessons.Select(l => new LessonShortDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    Status = context.UserProgresses
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
        var userId = userManager.GetUserId(User);

        var lesson = await context.Lessons
            .Include(l => l.Resources)
            .Include(l => l.Module)
            .ThenInclude(m => m!.Lessons)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null) return NotFound();

        var progress = await context.UserProgresses
            .FirstOrDefaultAsync(p => p.LessonId == id && p.UserId == userId);

        if (progress == null || progress.Status == ProgressStatus.Close)
        {
            TempData["Error"] = "Lecture is not available. Please complete previous materials.";
            return RedirectToAction("Index");
        }

        if (userId is null)
        {
            TempData["Error"] = "User not defined.";
            return RedirectToAction("Index");
        }

        if (progress.Status == ProgressStatus.Open) await progressService.LessonInProgressAsync(userId, lesson.Id);

        var homework = await context.Homeworks
            .FirstOrDefaultAsync(h => h.LessonId == id);

        if (homework != null)
        {
            ViewBag.HomeworkId = homework.Id;
            ViewBag.IsTestCompleted = await context.HomeworkSubmissions
                .AnyAsync(s => s.HomeworkId == homework.Id && s.StudentId == userId);
            ViewBag.Questions = await context.Questions
                .Include(q => q.Options)
                .Where(q => q.HomeworkId == homework.Id)
                .ToListAsync();

            if (ViewBag.IsTestCompleted)
            {
                var submission = await context.HomeworkSubmissions
                    .FirstOrDefaultAsync(s => s.HomeworkId == homework.Id && s.StudentId == userId);
                ViewBag.CurrentGrade = submission?.Grade ?? 0;
            }
        }
        else
        {
            ViewBag.HomeworkId = 0;
            ViewBag.IsTestCompleted = false;
            ViewBag.Questions = new List<Question>();
            ViewBag.CurrentGrade = 0;
        }

        var nextLessonId = lesson.Module!.Lessons!
            .OrderBy(l => l.LessonIndex)
            .FirstOrDefault(l => l.LessonIndex > lesson.LessonIndex)?.Id;

        if (nextLessonId == null)
        {
            var nextModule = await context.Modules
                .OrderBy(m => m.OrderIndex)
                .FirstOrDefaultAsync(m => m.OrderIndex > lesson.Module.OrderIndex);

            if (nextModule != null)
                nextLessonId = await context.Lessons
                    .Where(l => l.ModuleId == nextModule.Id)
                    .OrderBy(l => l.LessonIndex)
                    .Select(l => l.Id)
                    .FirstOrDefaultAsync();
        }

        ViewBag.NextLessonId = nextLessonId;

        ViewBag.ModuleProgress = await context.UserProgresses
            .Where(p => p.UserId == userId && p.ModuleId == lesson.ModuleId)
            .ToListAsync();

        var isCompleted = await context.HomeworkSubmissions
            .AnyAsync(s => s.StudentId == userId && s.Homework!.LessonId == id);

        ViewBag.IsTestCompleted = isCompleted || TempData["IsTestJustFinished"] != null;

        if (string.IsNullOrEmpty(lesson.Content)) lesson.Content = "{\"blocks\":[]}";

        return View(lesson);
    }

    // --- FIXED DOWNLOAD LOGIC FOR CLOUDINARY ---

    [HttpGet]
    public async Task<IActionResult> DownloadModuleMaterials(int moduleId)
    {
        var resources = await context.Resources
            .Include(r => r.Lesson)
            .Where(r => r.Lesson!.ModuleId == moduleId)
            .ToListAsync();

        if (!resources.Any()) return BadRequest("No materials found for this module.");

        var client = httpClientFactory.CreateClient();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var resource in resources) await AddResourceToArchive(archive, resource, client, "");
        }

        return File(memoryStream.ToArray(), "application/zip", $"Module_{moduleId}_Materials.zip");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFullArchive()
    {
        var resources = await context.Resources
            .Include(r => r.Lesson)
            .ThenInclude(l => l!.Module)
            .ToListAsync();

        if (!resources.Any()) return BadRequest("No materials found in the course.");

        var client = httpClientFactory.CreateClient();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var resource in resources)
            {
                var folderPath =
                    $"Module_{resource.Lesson!.Module!.OrderIndex}/Lesson_{resource.Lesson.LessonIndex}/";
                await AddResourceToArchive(archive, resource, client, folderPath);
            }
        }

        return File(memoryStream.ToArray(), "application/zip", "Full_Course_Archive.zip");
    }

    private async Task AddResourceToArchive(ZipArchive archive, Resource resource, HttpClient client,
        string folderPrefix)
    {
        byte[]? fileBytes = null;
        var entryName = folderPrefix + resource.FileName;

        if (resource.FilePath.StartsWith("http"))
        {
            try
            {
                fileBytes = await client.GetByteArrayAsync(resource.FilePath);
            }
            catch
            {
                // ignored
            }
        }
        else
        {
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials",
                resource.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(localPath)) fileBytes = await System.IO.File.ReadAllBytesAsync(localPath);
        }

        if (fileBytes != null)
        {
            var entry = archive.CreateEntry(entryName);
            await using var entryStream = entry.Open();
            await entryStream.WriteAsync(fileBytes);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetHomeworks()
    {
        var userId = userManager.GetUserId(User);
        var stats = await dashboardService.GetHomeworkStats(userId!);

        var executedHomeworks = await context.HomeworkSubmissions
            .Include(s => s.Homework)
            .ThenInclude(h => h!.Lesson)
            .ThenInclude(l => l!.Module)
            .Where(s => s.StudentId == userId)
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync();

        var course = await context.Courses.FirstOrDefaultAsync();
        var courseId = course?.Id ?? 0;

        if (courseId == 0)
            return View(new HomeworkDashboardDto
            {
                Stats = stats,
                ExecutedHomeworks = executedHomeworks,
                AllModules = [],
                AverageScore = executedHomeworks.Count != 0
                    ? executedHomeworks.Average(s => (double)(s.Grade ?? 0))
                    : 0.0
            });

        var activeLesson = await progressService.GetActiveLessonAsync(userId!, courseId);
        ViewBag.DebugLessonId = activeLesson?.Id.ToString() ?? "null";
        var modules = await context.Modules.OrderBy(m => m.OrderIndex).ToListAsync();

        Homework? currentHomework = null;
        if (activeLesson != null)
            currentHomework = await context.Homeworks
                .Include(h => h.Questions)
                .Include(h => h.Lesson)
                .FirstOrDefaultAsync(h => h.LessonId == activeLesson.Id);

        var viewModel = new HomeworkDashboardDto
        {
            Stats = stats,
            CurrentHomework = currentHomework,
            ExecutedHomeworks = executedHomeworks,
            AllModules = modules,
            AverageScore = executedHomeworks.Count != 0
                ? executedHomeworks.Average(s => (double)(s.Grade ?? 0))
                : 0.0
        };

        return View(viewModel);
    }
}