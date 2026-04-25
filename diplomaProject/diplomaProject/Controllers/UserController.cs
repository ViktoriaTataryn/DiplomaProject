using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Interfaces;
using diplomaProject.Models;
using diplomaProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplomaProject.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly AppDbContext _context;
        private readonly IProgressService _progressService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(IDashboardService dashboardService, AppDbContext context, IProgressService progressService, UserManager<ApplicationUser> userManager)
        {
            _dashboardService = dashboardService;
            _context = context;
            _progressService = progressService;
            _userManager = userManager;
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
            //if (course == 0)
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            bool progress = await _context.UserProgresses.AnyAsync(c => c.UserId == userId && c.CourseId == course);
            if (!progress)
            {
                await _progressService.StartCourse(userId, course);
            }
            // var course = 1;
            var data = await _dashboardService.GetDashboardView(userId, course);

            var lessons = await _context.Lessons.Include(l => l.Module)
        .Where(l => l.Module.CourseId == course)
        .OrderBy(l => l.LessonIndex)
        .ToListAsync();

            // Передаємо лекції та прогрес окремо
            ViewBag.Lessons = lessons;
            ViewBag.UserProgress = await _context.UserProgresses
                .Where(p => p.UserId == userId && p.CourseId == course)
                .ToListAsync();
            return View(data);
        }



        ////те що студент бачить після превірки адміна
        //[HttpGet]
        //public async Task<IActionResult> GetUserHomework()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var homeworks=await _context.HomeworkSubmissions
        //        .Include(u=>u.Homework)
        //        .ThenInclude(u=>u.Lesson)
        //        .Where(u=>u.StudentId==userId)
        //        .Where(s => _context.UserProgresses.Any(p =>
        //                p.UserId == userId &&
        //                p.LessonId == s.Homework.LessonId &&
        //                p.Status != ProgressStatus.Close))
        //        .OrderByDescending(u=>u.SubmissionDate)
        //        .ToListAsync();

        //    return View(homeworks);
        //}

        // GET: User/GetUserHomework
        [HttpGet]
        public async Task<IActionResult> GetUserHomework()
        {
            // Get current user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch homework submissions with related data
            var homeworks = await _context.HomeworkSubmissions
                .Include(u => u.Homework)
                    .ThenInclude(u => u.Lesson)
                .Where(u => u.StudentId == userId)
                // Only show homeworks for lessons that are not "Closed" in progress
                .Where(s => _context.UserProgresses.Any(p =>
                        p.UserId == userId &&
                        p.LessonId == s.Homework.LessonId &&
                        p.Status != ProgressStatus.Close))
                .OrderByDescending(u => u.SubmissionDate)
                .ToListAsync();

            return View(homeworks);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateData()
        {
            var user = await _userManager.GetUserAsync(User); //весь об'єкт ApplicationUser
            if (user == null) return NotFound();
            var data = new EditUserDTO
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserEmail = user.Email,
                UserPhone = user.PhoneNumber,
            };
            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateData(EditUserDTO editUser)
        {
            if (!ModelState.IsValid) return View(editUser);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = editUser.FirstName;
            user.LastName = editUser.LastName;
            user.PhoneNumber = editUser.UserPhone;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(editUser);

            }

            if (!string.IsNullOrEmpty(editUser.CurrentPassword) && !string.IsNullOrEmpty(editUser.NewPassword))
            {
                var passwordResult = await _userManager.ChangePasswordAsync(user, editUser.CurrentPassword, editUser.NewPassword);

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors) ModelState.AddModelError("", error.Description);
                    return View(editUser);
                }
            }

            TempData["SuccessMessage"] = "Профіль успішно оновлено!";
            return RedirectToAction("UpdateData");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(AddReviewDTO review)
        {
            if (!ModelState.IsValid)
            {
                return View(review);

                //var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                //return BadRequest(new { message = "Валідація не пройшла", details = errors }); //для постман

            }
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Користувач не авторизований. Перевірте токен.");
            }
            var userReview = new Review
            {
                Content = review.Content,
                Rating = review.Rating,
                UserId = userId,
            };
            _context.Reviews.Add(userReview);
            await _context.SaveChangesAsync();
            // return Ok(new { message = "Відгук додано успішно!", id = userReview.Id });//для постман
            return RedirectToAction("Index", "Home");
        }
    }
}
