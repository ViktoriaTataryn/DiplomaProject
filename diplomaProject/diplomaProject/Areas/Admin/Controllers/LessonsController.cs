using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyResource = diplomaProject.Models.Resource;

namespace diplomaProject.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class LessonsController(
    AppDbContext context,
    ICloudinaryService cloudinaryService,
    Cloudinary cloudinary)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string searchTerm)
    {
        var lessonQuery = context.Lessons.Include(l => l.Module).AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            lessonQuery = lessonQuery.Where(l => l.Title.ToLower().Contains(searchTerm));
        }

        var lessons = await lessonQuery
            .OrderBy(l => l.Module!.OrderIndex)
            .ThenBy(l => l.LessonIndex)
            .ToListAsync();

        // Заполняем "виртуальные" поля данными
        foreach (var lesson in lessons)
        {
            lesson.ModuleIndex = lesson.Module!.OrderIndex;
            lesson.UserCompletedNum = context.UserProgresses
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
    public async Task<IActionResult> Add()
    {
        var modules = await context.Modules
            .Select(m => new { m.Id, m.Title })
            .ToListAsync();

        // Передаємо список у SelectList
        ViewBag.ModuleList = new SelectList(modules, "Id", "Title");

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateLessonDto createLessonDto)
    {
        if (!ModelState.IsValid) return View(createLessonDto);

        var moduleExists = await context.Modules.AnyAsync(m => m.Id == createLessonDto.ModuleId);
        if (!moduleExists) return BadRequest("Вказаного модуля не існує. Будь ласка, перевірте ModuleId.");
        var lesson = new Lesson
        {
            Title = createLessonDto.Title,
            Description = createLessonDto.Description,
            Content = createLessonDto.Content,
            LessonIndex = createLessonDto.LessonIndex,
            ModuleId = createLessonDto.ModuleId,
            Resources = new List<MyResource>()
        };

        // Робота з Cloudinary
        if (createLessonDto.ResourceFiles != null && createLessonDto.ResourceFiles.Count > 0)
            foreach (var file in createLessonDto.ResourceFiles)
            {
                // Завантажуємо в хмару і отримуємо URL
                var cloudUrl = await cloudinaryService.UploadToCloudinary(file);

                lesson.Resources.Add(new MyResource
                {
                    FileName = file.FileName,
                    FilePath = cloudUrl
                });
            }

        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await context.Lessons
            .Include(l => l.Resources)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null) return NotFound();

        var userProgresses = context.UserProgresses.Where(p => p.LessonId == id);
        context.UserProgresses.RemoveRange(userProgresses);

        var homework = await context.Homeworks
            .Include(h => h.Questions)
            .ThenInclude(q => q.Options)
            .Include(h => h.Submissions) // Також видаляємо спроби студентів
            .FirstOrDefaultAsync(h => h.LessonId == id);

        if (homework != null)
            // Якщо є сабмішн з файлами, їх теж бажано видалити з Cloudinary тут (аналогічно ресурсам)
            context.Homeworks.Remove(homework);

        // 1. ВИДАЛЕННЯ З CLOUDINARY ЧЕРЕЗ REGEX (З контенту)
        if (!string.IsNullOrEmpty(lesson.Content))
        {
            var imgTags = Regex.Matches(lesson.Content, "<img.+?src=[\"'](.+?)[\"'].*?>");
            foreach (Match match in imgTags)
            {
                var url = match.Groups[1].Value;
                if (url.Contains("cloudinary.com"))
                {
                    var publicId = cloudinaryService.GetPublicIdFromUrl(url);
                    await cloudinary.DestroyAsync(new DeletionParams(publicId));
                }
            }
        }

        // 2. ВИДАЛЕННЯ РЕСУРСІВ З CLOUDINARY
        if (lesson.Resources.Count != 0)
            foreach (var resource in lesson.Resources)
            {
                if (resource.FilePath.Contains("cloudinary.com"))
                {
                    var publicId = cloudinaryService.GetPublicIdFromUrl(resource.FilePath);
                    // Видаляємо з хмари
                    await cloudinary.DestroyAsync(new DeletionParams(publicId));
                }

                context.Resources.Remove(resource);
            }

        context.Lessons.Remove(lesson);
        await context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var lesson = await context.Lessons
            .Include(l => l.Resources)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound($"Лекцію з id {id} не знайдено");
        var updateLesson = new UpdateLessonDto
        {
            Id = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            ModuleId = lesson.ModuleId,
            ExistingResources = lesson.Resources.ToList()
        };
        ViewBag.Modules = new SelectList(context.Modules, "Id", "Title", lesson.ModuleId);
        return View(updateLesson);
    }

    [HttpPost]
    public async Task<IActionResult> Update(UpdateLessonDto updateLessonDto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Modules = new SelectList(context.Modules, "Id", "Title", updateLessonDto.ModuleId);

            updateLessonDto.ExistingResources = await context.Resources
                .Where(r => r.LessonId == updateLessonDto.Id).ToListAsync();
            return View(updateLessonDto);
        }

        var lesson = await context.Lessons
            .Include(l => l.Resources)
            .FirstOrDefaultAsync(l => l.Id == updateLessonDto.Id);

        if (lesson == null) return NotFound();

        lesson.Title = updateLessonDto.Title;
        lesson.Description = updateLessonDto.Description;
        lesson.Content = updateLessonDto.Content;
        lesson.ModuleId = updateLessonDto.ModuleId;

        if (updateLessonDto.NewResourceFiles.Count > 0)
            foreach (var file in updateLessonDto.NewResourceFiles)
            {
                var cloudUrl = await cloudinaryService.UploadToCloudinary(file);

                lesson.Resources.Add(new MyResource
                {
                    FileName = file.FileName,
                    FilePath = cloudUrl,
                    LessonId = lesson.Id
                });
            }

        //await _context.SaveChangesAsync();
        try
        {
            await context.SaveChangesAsync();
        }
        catch
        {
            // Якщо помилка FK все одно виникає, ми побачимо це тут
            ModelState.AddModelError("", "Помилка бази даних: перевірте, чи обраний модуль існує.");
            ViewBag.Modules = new SelectList(context.Modules, "Id", "Title", updateLessonDto.ModuleId);
            return View(updateLessonDto);
        }

        return RedirectToAction("Index");
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
        if (file.Length == 0) return Json(new { success = 0 });

        try
        {
            // Використовуємо твій існуючий сервіс Cloudinary
            var cloudUrl = await cloudinaryService.UploadToCloudinary(file);

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
        try
        {
            // ВАЖЛИВО: У Cloudinary для аудіо ставимо ResourceType = "video"
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(audio.FileName, audio.OpenReadStream()),
                Folder = "ArtLine_Audio"
            };

            var uploadResult = await cloudinary.UploadAsync(uploadParams);
            var url = uploadResult.SecureUrl.ToString();

            return Json(new
            {
                success = 1,
                file = new { url }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = 0, message = ex.Message });
        }
    }

    public async Task<int> GetNumber()
    {
        var lessonNum = await context.Lessons.CountAsync();
        return lessonNum;
    }
}