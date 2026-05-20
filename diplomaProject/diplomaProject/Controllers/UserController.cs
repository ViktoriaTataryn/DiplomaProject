using System.Security.Claims;
using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Controllers;

[Authorize]
public class UserController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDashboardService _dashboardService;
    private readonly IProgressService _progressService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(IDashboardService dashboardService, AppDbContext context, IProgressService progressService,
        UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _dashboardService = dashboardService;
        _context = context;
        _progressService = progressService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: UserController
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); //тільки id користувача
        var course = await _context.CourseRegistrations
            .Where(c => c.UserId == userId)
            .Select(c => c.CourseId)
            .FirstOrDefaultAsync();
      
        var progress = await _context.UserProgresses.AnyAsync(c => c.UserId == userId && c.CourseId == course);
        if (!progress) await _progressService.StartCourse(userId!, course);
     
        var data = await _dashboardService.GetDashboardView(userId!, course);

        var lessons = await _context.Lessons.Include(l => l.Module)
            .Where(l => l.Module!.CourseId == course)
            .OrderBy(l => l.LessonIndex)
            .ToListAsync();

        // Передаємо лекції та прогрес окремо
        ViewBag.Lessons = lessons;
        ViewBag.UserProgress = await _context.UserProgresses
            .Where(p => p.UserId == userId && p.CourseId == course)
            .ToListAsync();
        return View(data);
    }


    [HttpGet]
    public async Task<IActionResult> UpdateData()
    {
        var user = await _userManager.GetUserAsync(User); //весь об'єкт ApplicationUser
        if (user == null) return NotFound();
        var registerData = await _context.CourseRegistrations.FirstOrDefaultAsync(u => u.UserId == user.Id);
        var lastActivity = await _context.UserProgresses.Where(u => u.UserId == user.Id)
            .OrderByDescending(l => l.LastActivity).FirstOrDefaultAsync();
        var data = new UserProfileDto
        {
            RegistrationDate = user.RegistrationDate,
            IsPaid = registerData?.IsPaid ?? false,
            LastActivity = lastActivity?.LastActivity ?? DateTime.MinValue,
            PaymentDate = registerData?.PaymentDate ?? DateTime.MinValue,
            EditModel = new EditUserDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserEmail = user.Email!,
                UserPhone = user.PhoneNumber!
            }
        };
        return View(data);
    }




    [HttpPost]
    public async Task<IActionResult> UpdateData([Bind(Prefix = "EditModel")] EditUserDto editUser)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        // 1. Спроба змінити пароль
        IdentityResult? passwordResult = null;
        var isPasswordChangeRequested = !string.IsNullOrEmpty(editUser.CurrentPassword) &&
                                        !string.IsNullOrEmpty(editUser.NewPassword);

        if (isPasswordChangeRequested)
            passwordResult =
                await _userManager.ChangePasswordAsync(user, editUser.CurrentPassword!, editUser.NewPassword!);

      
        if (!ModelState.IsValid || (isPasswordChangeRequested && passwordResult?.Succeeded != true))
        {
            if (passwordResult != null && !passwordResult.Succeeded)
                foreach (var error in passwordResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

         
            var registerData = await _context.CourseRegistrations.FirstOrDefaultAsync(u => u.UserId == user.Id);
            var lastActivity = await _context.UserProgresses
                .Where(u => u.UserId == user.Id)
                .OrderByDescending(l => l.LastActivity)
                .FirstOrDefaultAsync();

            var profileData = new UserProfileDto
            {
                RegistrationDate = user.RegistrationDate,
                IsPaid = registerData?.IsPaid ?? false,
                LastActivity = lastActivity?.LastActivity ?? DateTime.MinValue,
                PaymentDate = registerData?.PaymentDate ?? DateTime.MinValue,
                EditModel = editUser 
            };

            return View("UpdateData", profileData); 
        }

       
        user.FirstName = editUser.FirstName;
        user.LastName = editUser.LastName;
        user.PhoneNumber = editUser.UserPhone;

        await _userManager.UpdateAsync(user);

        if (isPasswordChangeRequested && passwordResult?.Succeeded == true)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Пароль та дані успішно оновлено!";
        }
        else
        {
            TempData["SuccessMessage"] = "Дані профілю успішно змінено!";
        }

        return RedirectToAction("UpdateData");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(AddReviewDto review)
    {
        //var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
        //return BadRequest(new { message = "Валідація не пройшла", details = errors }); //для постман
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized("Користувач не авторизований. Перевірте токен.");
        var userReview = new Review
        {
            Content = review.Content,
            Rating = review.Rating,
            UserId = userId
        };
        _context.Reviews.Add(userReview);
        await _context.SaveChangesAsync();
        // return Ok(new { message = "Відгук додано успішно!", id = userReview.Id });//для постман
        return RedirectToAction("Index", "Home", new { area = "" });
    }
}