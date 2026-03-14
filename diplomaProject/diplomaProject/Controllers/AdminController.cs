using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Models;
using diplomaProject.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProgressService _progressService;

        public AdminController(AppDbContext context, ProgressService progressService)
        {
            _context = context;
            _progressService = progressService;
        }

        // GET: AdminController
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStudents()
        {
            var students = await _context.Users.ToListAsync();
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> GetHomeworkSubmission()
        {
            var tasks = await _context.HomeworkSubmissions.Include(t=>t.Student)
                .Include(t=>t.Homework)
                .ThenInclude(h=>h.Lesson)
                .Where(t=>t.Status==HomeworkStatus.Pending)
                .OrderBy(t=>t.SubmissionDate)
                .ToListAsync();

            return View(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHomework(int homeworkId, int? Grade, string Feedback, string status)
        {
            var homework = await _context.HomeworkSubmissions.FindAsync(homeworkId);
            if (homework == null)
            {
                return NotFound();
            }
            homework.Grade = Grade;
            homework.Feedback = Feedback;

            if (Grade.HasValue && Grade.Value > 0)
            {
                homework.Status = HomeworkStatus.Approved;
                await _progressService.UnlockNextLessonAsync(homework.StudentId,homework.Homework.LessonId);
            }
            else
            {
                homework.Status = HomeworkStatus.Rejected;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GetHomeworkSubmission));
        }

        //[HttpPost]
        //public async Task<IActionResult> AddLesson(CreateLessonDTO createLessonDTO)
        //{
         
        //        if (!ModelState.IsValid) return View(createLessonDTO);
        //        var lesson = new Lesson
        //        {
        //            Title = createLessonDTO.Title,
        //            Description = createLessonDTO.Description,
        //            Content = createLessonDTO.Content
        //        };
        //        if (!string.IsNullOrWhiteSpace(createLessonDTO.HomeworkDescription)) {
        //            var homework = new Homework
        //            {
        //                Description = createLessonDTO.HomeworkDescription,
        //                Lesson = lesson
        //            };
        //        }
        //        if (createLessonDTO.ResourceFiles != null && createLessonDTO.ResourceFiles.Count > 0)
        //        {
        //            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "resources");
        //            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        //            foreach (var file in createLessonDTO.ResourceFiles)
        //            {
        //                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        //                var fullPath = Path.Combine(folderPath, fileName);

        //                // Зберігаємо файл на диск
        //                using (var stream = new FileStream(fullPath, FileMode.Create))
        //                {
        //                    await file.CopyToAsync(stream);
        //                }

        //                lesson.Resources.Add(new Resource
        //                {
        //                    FileName = file.FileName,
        //                    FilePath = "/resources/" + fileName
        //                });
        //            }
        //        }
        //        _context.Add(lesson);
        //        await _context.SaveChangesAsync();
        //       // return RedirectToAction("Index");

        //    return Ok(new { message = "Лекція успішно створена!", lessonId = lesson.Id });
        //}
            
    }
}
