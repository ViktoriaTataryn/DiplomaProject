using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class StudentsController(AppDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string searchTerm)
    {
        var studentsQuery = context.UserProgresses
            .Include(s => s.User)
            .Include(s => s.Lesson)
            .Where(l => l.Status == ProgressStatus.InProgress)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim().ToLower();
            studentsQuery = studentsQuery.Where(u => u.User!.LastName.ToLower().Contains(searchTerm)
                                                     || u.User.FirstName.ToLower().Contains(searchTerm));
        }

        // Мы вытаскиваем из прогресса именно объекты User, так как GetStudents.cshtml ожидает IEnumerable<ApplicationUser>
        var students = await studentsQuery
            .Select(s => s.User)
            .Distinct()
            .ToListAsync();

        ViewBag.CurrentSearch = searchTerm;
        return View(students);
    }


    [HttpPost]
    [ValidateAntiForgeryToken] // Захист від підробки запитів
    public async Task<IActionResult> Delete(string id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null) return NotFound();


        var userProgress = context.UserProgresses.Where(p => p.UserId == id);
        context.UserProgresses.RemoveRange(userProgress);
        context.Users.Remove(user);

        await context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}