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
            _dashboardService =dashboardService;
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
            if (course == 0)
            {
                return RedirectToAction("Index", "Home");
            }
            bool progress = await _context.UserProgresses.AnyAsync(c => c.UserId == userId&&c.CourseId==course);
            if (!progress)
            {
                await _progressService.StartCourse(userId, course);
            }
           // var course = 1;
            var data =await _dashboardService.GetDashboardView(userId,course);
            return View(data);
        }



        [HttpGet]
        public async Task<IActionResult> GetHomeworkSubmission()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var homeworks=await _context.HomeworkSubmissions
                .Include(u=>u.Homework)
                .ThenInclude(u=>u.Lesson)
                .Where(u=>u.StudentId==userId)
                .Where(s => _context.UserProgresses.Any(p =>
                        p.UserId == userId &&
                        p.LessonId == s.Homework.LessonId &&
                        p.Status != ProgressStatus.Close))
                .OrderByDescending(u=>u.SubmissionDate)
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
            if(!ModelState.IsValid) return View(editUser);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FirstName = editUser.FirstName;
            user.LastName = editUser.LastName;
            user.PhoneNumber = editUser.UserPhone;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) {
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
            }
            var userId = _userManager.GetUserId(User);
           
            var userReview = new Review
            {
                Content=review.Content,
                Rating=review.Rating,
                UserId = userId,
            };
             _context.Reviews.Add(userReview);
           await _context.SaveChangesAsync();

            return RedirectToAction("Index","Home");
           }
    }
}
