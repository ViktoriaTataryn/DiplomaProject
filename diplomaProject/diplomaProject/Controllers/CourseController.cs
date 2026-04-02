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
        // Позволяет получить список файлов отдельно, если нужно
        [HttpGet]
        public async Task<IActionResult> GetResources(int lessonId)
        {
            var resources = await _context.Resources
                .Where(r => r.LessonId == lessonId)
                .ToListAsync();
            return PartialView("_ResourcesPartial", resources);
        }


        //// 4. Отправка домашки через POST (без отдельной вьюшки)
        //[HttpPost]
        //public async Task<IActionResult> SubmitHomework(int homeworkId, string homeworkText)
        //{
        //    // Тут в будущем будет логика сохранения в базу через ProgressService Вики
        //    // Пока просто делаем заглушку, чтобы кнопка работала

        //    if (string.IsNullOrWhiteSpace(homeworkText))
        //    {
        //        ModelState.AddModelError("", "Текст завдання не може бути порожнім.");
        //        return RedirectToAction( "Lesson", new { id = homeworkId }); // Повертаємо на сторінку уроку

        //    }
        //    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "submissions");
        //    if (!Directory.Exists(folderPath))
        //    {
        //        Directory.CreateDirectory(folderPath);
        //    }
           
        //    var fileName = $"homework_{homeworkId}_{Guid.NewGuid()}.txt";
        //    var fullPath = Path.Combine(folderPath, fileName);

        //    //  Записуємо текст у файл
        //    await System.IO.File.WriteAllTextAsync(fullPath, homeworkText);

        //    var userId = _userManager.GetUserId(User);

        //    var submission = new HomeworkSubmission
        //    {
        //        HomeworkId = homeworkId,
        //        StudentId = userId,
        //        FilePath = "/submissions/" + fileName,
        //        SubmissionDate = DateTime.Now,
        //        Status = HomeworkStatus.Pending
        //    };

        //    _context.HomeworkSubmissions.Add(submission);
        //    await _context.SaveChangesAsync();

        //    TempData["Message"] = "Homework submitted successfully!";
        //    return RedirectToAction("Lesson");
        //}

        // 5. Auto-grading homework validation
        [HttpPost]
        public async Task<IActionResult> CheckHomework(int lessonId, List<int> selectedOptionIds)
        {
            // Fetch questions for this lesson, including their options
            var questions = await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.LessonId == lessonId)
                .ToListAsync();

            int score = 0;
            int maxScore = questions.Count;

            // Loop through each question to calculate points
            foreach (var question in questions)
            {
                // Array 1: Extract the array of CORRECT IDs from the database
                var correctOptionIds = question.Options
                    .Where(o => o.IsCorrect)
                    .Select(o => o.Id)
                    .ToList();

                // Array 2: Extract the array of STUDENT's selected IDs for THIS question
                var studentIdsForThisQuestion = selectedOptionIds
                    .Intersect(question.Options.Select(o => o.Id))
                    .ToList();

                // Compare the two arrays (Strict check for exact match)
                bool isAnswerPerfect = !correctOptionIds.Except(studentIdsForThisQuestion).Any() &&
                                       !studentIdsForThisQuestion.Except(correctOptionIds).Any();

                if (isAnswerPerfect)
                {
                    score++;
                }
            }

            // Save the score to TempData to show on the page
            TempData["HomeworkResult"] = $"Your score: {score} out of {maxScore}";

            return RedirectToAction("Lesson", new { id = lessonId });
        }
    }
}