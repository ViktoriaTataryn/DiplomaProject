using diplomaProject.Data;
using diplomaProject.Interfaces;
using diplomaProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace diplomaProject.Controllers
{
    public class UserController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly AppDbContext _context;

        public UserController(IDashboardService dashboardService, AppDbContext context)
        {
            _dashboardService =dashboardService;
            _context = context;
        }


        // GET: UserController
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
           var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var course = await _context.CourseRegistrations
                .Where(c => c.UserId == userId)
                .Select(c => c.CourseId)
                .FirstOrDefaultAsync();
            if (course == 0)
            {
                return RedirectToAction("Index", "Home");
            }
           // var course = 1;
            var data =await _dashboardService.GetDashboardView(userId,course);
            return View(data);
        }



        // GET: UserController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
