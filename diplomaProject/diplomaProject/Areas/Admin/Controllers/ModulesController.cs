using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class ModulesController(AppDbContext context, ICloudinaryService cloudinaryService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string searchTerm)
    {
        var modulesQuery = context.Modules.AsQueryable();
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            modulesQuery = modulesQuery.Where(m => m.Title.ToLower().Contains(searchTerm));
        }

        var modules = await modulesQuery
            .OrderBy(m => m.CourseId)
            .ThenBy(m => m.OrderIndex)
            .ToListAsync();

        ViewBag.CurrentSearch = searchTerm;

        return View(modules);

        // return Json(modules); постман
    }

    [HttpGet]
    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(CreateModuleDto createModule)
    {
        if (!ModelState.IsValid) return View(createModule);
        var indexExist = await context.Modules.AnyAsync(x => x.OrderIndex == createModule.OrderIndex);
        if (indexExist)
        {
            ModelState.AddModelError("OrderIndex", "Модуль з таким номером уже існує.");
            return View(createModule);
        }

        var course = await context.Courses.FirstOrDefaultAsync();
        var courseId = course?.Id ?? 0;

        var module = new Module
        {
            Title = createModule.Title,
            OrderIndex = createModule.OrderIndex,
            Description = createModule.Description,
            CourseId = courseId,
            ImageUrl = createModule.ImageFile is { Length: > 0 }
                ? await cloudinaryService.UploadToCloudinary(createModule.ImageFile)
                : null
        };

        context.Add(module);
        await context.SaveChangesAsync();
        return RedirectToAction("Index");

        // return Ok(new { message = "Модуль успішно створений!", moduleId = module.Id });  постман
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var module = await context.Modules
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (module == null) return NotFound($"Модуль з ID {id} не знайдено.");

        return View(module);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var module = await context.Modules
            .Include(m => m.Lessons)!
            .ThenInclude(l => l.Resources)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (module == null) return RedirectToAction("Index");
        if (!string.IsNullOrEmpty(module.ImageUrl))
        {
            var publicId = cloudinaryService.GetPublicIdFromUrl(module.ImageUrl);
            await cloudinaryService.DeleteFromCloudinary(publicId);
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        foreach (var lesson in module.Lessons!)
        {
            foreach (var resource in lesson.Resources)
            {
                var resPublicId = cloudinaryService.GetPublicIdFromUrl(resource.FilePath);
                await cloudinaryService.DeleteFromCloudinary(resPublicId);
                context.Resources.Remove(resource);
            }

            foreach (var userProgress in await context.UserProgresses.Where(up => up.LessonId == lesson.Id)
                         .ToListAsync())
                context.UserProgresses.Remove(userProgress);

            context.Lessons.Remove(lesson);
        }

        foreach (var userProgress in await context.UserProgresses.Where(up => up.ModuleId == module.Id).ToListAsync())
            context.UserProgresses.Remove(userProgress);

        context.Modules.Remove(module);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var module = await context.Modules
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (module == null) return NotFound($"Модуль з ID {id} не знайдено.");
        var updateModule = new UpdateModuleDto
        {
            Id = id,
            Title = module.Title,
            Description = module.Description,
            OrderIndex = module.OrderIndex,
            ImageForUser = module.ImageUrl,
            LessonNames = module.Lessons!.OrderBy(l => l.LessonIndex).Select(l => l.Title).ToList()
        };
        return View(updateModule);
    }

    [HttpPost]
    public async Task<IActionResult> Update(UpdateModuleDto updateModuleDto)
    {
        if (!ModelState.IsValid) return View(updateModuleDto);

        await using var transaction = await context.Database.BeginTransactionAsync();
        var module = await context.Modules
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == updateModuleDto.Id);

        if (module == null) return NotFound($"Модуль з ID {updateModuleDto.Id} не знайдено.");

        if (updateModuleDto.ImageFile is { Length: > 0 })
        {
            // Видаляємо стару картинку, якщо вона була
            if (!string.IsNullOrEmpty(module.ImageUrl))
            {
                var oldPublicId = cloudinaryService.GetPublicIdFromUrl(module.ImageUrl);
                await cloudinaryService.DeleteFromCloudinary(oldPublicId);
            }

            // Завантажуємо нову
            module.ImageUrl = await cloudinaryService.UploadToCloudinary(updateModuleDto.ImageFile);
        }

        module.Title = updateModuleDto.Title;
        module.Description = updateModuleDto.Description;
        module.OrderIndex = updateModuleDto.OrderIndex;

        foreach (var lesson in module.Lessons!)
        {
            lesson.LessonIndex = updateModuleDto.LessonNames.IndexOf(lesson.Title);
            context.Lessons.Update(lesson);
        }

        context.Modules.Update(module);
        TempData["Success"] = "МЕТОД ДІЙШОВ ДО ЗБЕРЕЖЕННЯ!";
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
        return RedirectToAction("Index");
    }


    public async Task<int> GetNumber()
    {
        var moduleNum = await context.Modules.CountAsync();
        return moduleNum;
    }
}