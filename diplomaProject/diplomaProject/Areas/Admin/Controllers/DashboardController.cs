using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class DashboardController(
    AppDbContext context,
    UserManager<ApplicationUser> userManager)
    : Controller
{
    // GET: AdminController

    // правильная модель для отображения на дашборде, но так как в модели Course нет свойства Price (по которому можно посчитать доход), то я временно закомментировал этот код и поставил статичную цифру для дохода   
    //public async Task<IActionResult> Index()
    //{
    //    // 1. Считаем общее количество студентов
    //    // var students = await _userManager.GetUsersInRoleAsync("Student");
    //    // ViewBag.TotalStudents = students.Count;

    //    // 2. Считаем общий доход (сумма цен всех купленных курсов)
    //    // var totalRevenue = _context.CourseRegistrations
    //    //     .Join(_context.Courses, reg => reg.CourseId, c => c.Id, (reg, c) => c.Price)
    //    //     .Sum();
    //    // ViewBag.TotalRevenue = totalRevenue;

    //    // 3. Последняя активность (например, последние 5 сданных ДЗ)
    //    // var recentActivity = _context.HomeworkSubmissions
    //    //     .OrderByDescending(h => h.SubmittedAt)
    //    //     .Take(5)
    //    //     .Select(h => new {
    //    //         UserName = _context.Users.Where(u => u.Id == h.StudentId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault(),
    //    //         Message = "здав(ла) домашнє завдання",
    //    //         Date = h.SubmittedAt
    //    //     }).ToList();

    //    // ViewBag.RecentActivity = recentActivity;

    //    // return View();
    //}

    // ИСПРАВЛЕНИЕ: Заменил SubmittedAt на SubmissionDate, так как в модели HomeworkSubmission нет свойства SubmittedAt
    // GET: AdminController
    public async Task<IActionResult> Index()
    {
        // 1. Считаем общее количество студентов
        var students = await userManager.GetUsersInRoleAsync("Student");
        ViewBag.TotalStudents = students.Count;

        // 2. Успішність виконання ДЗ (замість доходу)
        // Беремо всі ДЗ і рахуємо відсоток тих, які перевірені (мають оцінку)
        var totalHomeworks = await context.HomeworkSubmissions.CountAsync();
        var gradedHomeworks = await context.HomeworkSubmissions.CountAsync(h => h.Grade != null);

        ViewBag.HomeworkSuccessRate = totalHomeworks > 0
            ? (int)Math.Round((double)gradedHomeworks / totalHomeworks * 100)
            : 0;

        // 3. Последняя активность
        var recentActivity = context.HomeworkSubmissions
            .OrderByDescending(h => h.SubmissionDate)
            .Take(5)
            .Select(h => new
            {
                UserName = context.Users.Where(u => u.Id == h.StudentId).Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault(),
                Message = "здав(ла) домашнє завдання",
                Date = h.SubmissionDate
            }).ToList();

        ViewBag.RecentActivity = recentActivity;

        return View();
    }
}