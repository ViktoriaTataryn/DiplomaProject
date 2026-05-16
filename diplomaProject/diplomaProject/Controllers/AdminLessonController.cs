using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using diplomaProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using MyResource = diplomaProject.Models.Resource;

namespace diplomaProject.Controllers
{
    [Authorize(Roles = "Admin")]

    public class AdminLessonController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProgressService _progressService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly Cloudinary _cloudinary;

        public AdminLessonController(AppDbContext context, ProgressService progressService, UserManager<ApplicationUser> userManager, ICloudinaryService cloudinaryService, Cloudinary cloudinary)
        {
            _context = context;
            _progressService = progressService;
            _userManager = userManager;
            _cloudinaryService = cloudinaryService;
            _cloudinary = cloudinary;
        }


        [HttpGet]
        public async Task<IActionResult> GetLessons(string searchTerm)
        {
            var lessonQuery = _context.Lessons.Include(l => l.Module).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                lessonQuery = lessonQuery.Where(l => l.Title.ToLower().Contains(searchTerm));
            }

            var lessons = await lessonQuery
                .OrderBy(l => l.Module.OrderIndex)
                .ThenBy(l => l.LessonIndex)
                .ToListAsync();

            // Заполняем "виртуальные" поля данными
            foreach (var lesson in lessons)
            {
                lesson.ModuleIndex = lesson.Module.OrderIndex;
                lesson.UserCompletedNum = _context.UserProgresses
                    .Count(u => u.LessonId == lesson.Id && u.Status == ProgressStatus.Completed);
            }

            ViewBag.CurrentSearch = searchTerm;
            return View(lessons); // Теперь возвращаем Model, а не DTO
        }
        //public async Task<IActionResult> GetLessons(string searchTerm)
        //{
        //    var lessonQuery = _context.Lessons.Include(l => l.Module).AsQueryable();
        //    if (!string.IsNullOrEmpty(searchTerm))
        //    {
        //        searchTerm = searchTerm.Trim().ToLower();
        //        lessonQuery = lessonQuery.Where(l => l.Title.ToLower().Contains(searchTerm));
        //    }

        //    var lessons = await lessonQuery
        //        .OrderBy(l => l.Module.OrderIndex)
        //        .ThenBy(l => l.LessonIndex)
        //      .Select(l => new LessonDTO
        //      {
        //          Id = l.Id,
        //          Title = l.Title,
        //          ModuleIndex = l.Module.OrderIndex,
        //          UserCompletedNum = _context.UserProgresses.Count(u => u.LessonId == l.Id && u.Status == ProgressStatus.Completed),
        //      })
        //      .ToListAsync();

        //    ViewBag.CurrentSearch = searchTerm;

        //    return View(lessons);

        //    //return Json(lessons); //постман
        //}

        [HttpGet]
        public async Task<IActionResult> AddLesson()
        {
            var modules = await _context.Modules
                .Select(m => new { m.Id, m.Title })
                .ToListAsync();

            // Передаємо список у SelectList
            ViewBag.ModuleList = new SelectList(modules, "Id", "Title");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddLesson(CreateLessonDTO createLessonDTO)
        {
            if (!ModelState.IsValid) return View(createLessonDTO);

            var moduleExists = await _context.Modules.AnyAsync(m => m.Id == createLessonDTO.ModuleId);
            if (!moduleExists)
            {
                return BadRequest("Вказаного модуля не існує. Будь ласка, перевірте ModuleId.");
            }
            var lesson = new Lesson
            {
                Title = createLessonDTO.Title,
                Description = createLessonDTO.Description,
                Content = createLessonDTO.Content,
                LessonIndex = createLessonDTO.LessonIndex,
                ModuleId = createLessonDTO.ModuleId,
                Resources = new List<MyResource>()
            };

            // Робота з Cloudinary
            if (createLessonDTO.ResourceFiles != null && createLessonDTO.ResourceFiles.Count > 0)
            {
                foreach (var file in createLessonDTO.ResourceFiles)
                {
                    // Завантажуємо в хмару і отримуємо URL
                    var cloudUrl = await _cloudinaryService.UploadToCloudinary(file);

                    lesson.Resources.Add(new MyResource
                    {
                        FileName = file.FileName,
                        FilePath = cloudUrl
                    });
                }
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetLessons");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLesson(int Id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == Id);

            if (lesson == null) return NotFound();

            var userProgresses = _context.UserProgresses.Where(p => p.LessonId == Id);
            _context.UserProgresses.RemoveRange(userProgresses);

            var homework = await _context.Homeworks
        .Include(h => h.Questions)
            .ThenInclude(q => q.Options)
        .Include(h => h.Submissions) // Також видаляємо спроби студентів
        .FirstOrDefaultAsync(h => h.LessonId == Id);

            if (homework != null)
            {
                // Якщо є сабмішн з файлами, їх теж бажано видалити з Cloudinary тут (аналогічно ресурсам)
                _context.Homeworks.Remove(homework);
            }

            // 1. ВИДАЛЕННЯ З CLOUDINARY ЧЕРЕЗ REGEX (З контенту)
            if (!string.IsNullOrEmpty(lesson.Content))
            {
                var imgTags = Regex.Matches(lesson.Content, "<img.+?src=[\"'](.+?)[\"'].*?>");
                foreach (Match match in imgTags)
                {
                    var url = match.Groups[1].Value;
                    if (url.Contains("cloudinary.com"))
                    {
                        var publicId = _cloudinaryService.GetPublicIdFromUrl(url);
                        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    }
                }
            }

            // 2. ВИДАЛЕННЯ РЕСУРСІВ З CLOUDINARY
            if (lesson.Resources != null && lesson.Resources.Any())
            {
                foreach (var resource in lesson.Resources)
                {
                    if (resource.FilePath.Contains("cloudinary.com"))
                    {
                        var publicId = _cloudinaryService.GetPublicIdFromUrl(resource.FilePath);
                        // Видаляємо з хмари
                        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    }
                    _context.Resources.Remove(resource);
                }
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetLessons");
        }

        [HttpGet]
        public async Task<IActionResult> UpdateLesson(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (lesson == null)
            {
                return NotFound($"Лекцію з id {id} не знайдено");
            }
            var updateLesson = new UpdateLessonDTO
            {
                Id = lesson.Id,
                Title = lesson.Title,
                Description = lesson.Description,
                Content = lesson.Content,
                ModuleId = lesson.ModuleId,
                ExistingResources = lesson.Resources.ToList()

            };
            ViewBag.Modules = new SelectList(_context.Modules, "Id", "Title", lesson.ModuleId);
            return View(updateLesson);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLesson(UpdateLessonDTO updateLessonDTO)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Modules = new SelectList(_context.Modules, "Id", "Title", updateLessonDTO.ModuleId);

                updateLessonDTO.ExistingResources = await _context.Resources
                    .Where(r => r.LessonId == updateLessonDTO.Id).ToListAsync();
                return View(updateLessonDTO);
            }
            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == updateLessonDTO.Id);

            if (lesson == null)
            {
                return NotFound();
            }

            lesson.Title = updateLessonDTO.Title;
            lesson.Description = updateLessonDTO.Description;
            lesson.Content = updateLessonDTO.Content;
            lesson.ModuleId = updateLessonDTO.ModuleId;

            if (updateLessonDTO.NewResourceFiles != null && updateLessonDTO.NewResourceFiles.Count > 0)
            {
                foreach (var file in updateLessonDTO.NewResourceFiles)
                {
                    var cloudUrl = await _cloudinaryService.UploadToCloudinary(file);

                    lesson.Resources.Add(new MyResource
                    {
                        FileName = file.FileName,
                        FilePath = cloudUrl,
                        LessonId = lesson.Id
                    });
                }
            }

            //await _context.SaveChangesAsync();
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Якщо помилка FK все одно виникає, ми побачимо це тут
                ModelState.AddModelError("", "Помилка бази даних: перевірте, чи обраний модуль існує.");
                ViewBag.Modules = new SelectList(_context.Modules, "Id", "Title", updateLessonDTO.ModuleId);
                return View(updateLessonDTO);
            }
            return RedirectToAction("GetLessons");

        }

        //[HttpPost]
        //[IgnoreAntiforgeryToken]
        //public async Task<IActionResult> UploadImage(IFormFile upload) 
        //{
        //    if (upload == null || upload.Length == 0)
        //    {
        //        return Json(new { error = new { message = "Файл не отримано сервером." } });
        //    }

        //    try
        //    {
        //        // 1. Завантажуємо в Cloudinary
        //        var cloudUrl = await _cloudinaryService.UploadToCloudinary(upload);

        //        // 2. ВАЖЛИВО: CKEditor 5 очікує саме такий JSON
        //        return Ok(new
        //        {
        //            uploaded = true,
        //            url = cloudUrl
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { uploaded = false, error = new { message = ex.Message } });
        //    }
        //}
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = 0 });
            }

            try
            {
                // Використовуємо твій існуючий сервіс Cloudinary
                var cloudUrl = await _cloudinaryService.UploadToCloudinary(file);

                // Editor.js вимагає саме таку структуру відповіді:
                return Json(new
                {
                    success = 1,
                    file = new { url = cloudUrl }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = 0, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UploadAudio(IFormFile audio)
        {
            if (audio == null) return Json(new { success = 0 });

            try
            {
                // ВАЖЛИВО: У Cloudinary для аудіо ставимо ResourceType = "video"
                var uploadParams = new VideoUploadParams()
                {
                    File = new FileDescription(audio.FileName, audio.OpenReadStream()),
                    Folder = "ArtLine_Audio"
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                var url = uploadResult.SecureUrl.ToString();

                return Json(new
                {
                    success = 1,
                    file = new { url = url }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = 0, message = ex.Message });
            }
        }
    }
}
