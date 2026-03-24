using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using diplomaProject.Data;
using diplomaProject.DTOs;
using diplomaProject.Models;
using diplomaProject.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;


namespace diplomaProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProgressService _progressService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Cloudinary _cloudinary;

        public AdminController(AppDbContext context, ProgressService progressService, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment, Cloudinary cloudinary)
        {
            _context = context;
            _progressService = progressService;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _cloudinary = cloudinary;
        }

        // GET: AdminController
        public ActionResult Index()
        {
            return View();
        }
       
        [HttpGet]
        public async Task<IActionResult> GetStudents(string searchTerm)
        {
            var studentsQuery =  _context.UserProgresses
                .Include(s=>s.User)
                .Include(s=>s.Lesson)
                .Where(l=>l.Status==ProgressStatus.InProgress)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                studentsQuery = studentsQuery.Where(u=>u.User.LastName.ToLower().Contains(searchTerm)
                || u.User.FirstName.ToLower().Contains(searchTerm));
            }
            var students = studentsQuery.ToList();
            ViewBag.CurrentSearch = searchTerm;
            //return View(students);
             return Json(students); //постман
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost, ActionName("DeleteUserConfirmed")]
        [ValidateAntiForgeryToken] // Захист від підробки запитів
        public async Task<IActionResult> DeleteUserConfirmed(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

          
            var userProgress = _context.UserProgresses.Where(p => p.UserId == userId);
            _context.UserProgresses.RemoveRange(userProgress);
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            return RedirectToAction("GetStudents");
        }


        //[HttpGet]
        //public async Task<IActionResult> GetHomeworkSubmission()
        //{
        //    var tasks = await _context.HomeworkSubmissions.Include(t=>t.Student)
        //        .Include(t=>t.Homework)
        //        .ThenInclude(h=>h.Lesson)
        //        .Where(t=>t.Status==HomeworkStatus.Pending)
        //        .OrderBy(t=>t.SubmissionDate)
        //        .ToListAsync();

        //    return View(tasks);
        //}

        //[HttpPost]
        //public async Task<IActionResult> UpdateHomework(int homeworkId, int? Grade, string Feedback, string status)
        //{
        //    var homework = await _context.HomeworkSubmissions.FindAsync(homeworkId);
        //    if (homework == null)
        //    {
        //        return NotFound();
        //    }
        //    homework.Grade = Grade;
        //    homework.Feedback = Feedback;

        //    if (Grade.HasValue && Grade.Value > 0)
        //    {
        //        homework.Status = HomeworkStatus.Approved;
        //        await _progressService.UnlockNextLessonAsync(homework.StudentId,homework.Homework.LessonId);
        //    }
        //    else
        //    {
        //        homework.Status = HomeworkStatus.Rejected;
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(GetHomeworkSubmission));
        //}

        [HttpPost]
        public async Task<IActionResult> AddModule(CreateModuleDTO createModule)
        {
            if (!ModelState.IsValid) return View(createModule);
            var indexExist =await _context.Modules.AnyAsync(x => x.OrderIndex == createModule.OrderIndex);
            if (indexExist)
            {
                ModelState.AddModelError("OrderIndex", "Модуль з таким номером уже існує.");
                return View(createModule);
            }
            var module = new Models.Module
            {
                Title = createModule.Title,
                OrderIndex = createModule.OrderIndex,
                Description = createModule.Description,
                CourseId = createModule.CourseId
            };
            
            _context.Add(module);
            await _context.SaveChangesAsync();
             return RedirectToAction("Index");

           // return Ok(new { message = "Модуль успішно створений!", moduleId = module.Id });  постман
        }

        [HttpGet]
        public IActionResult AddModule()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetModules(string searchTerm)
        {
            var modulesQuery = _context.Modules.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower(); 
                modulesQuery = modulesQuery.Where(m => m.Title.ToLower().Contains(searchTerm));
            }

            var modules = await modulesQuery
                .OrderBy(m=>m.OrderIndex)
              .Select( m => new ModuleDTO
            {
                Id = m.Id,
                Title = m.Title,
                  LessonsNum= m.Lessons.Count(),
                  //LessonsNum = 1,
                  UserCompletedNum = _context.UserProgresses.Count(u=>u.ModuleId==m.Id&& u.Status==ProgressStatus.Completed),
            })
              .ToListAsync();

            ViewBag.CurrentSearch = searchTerm;

             return View(modules);

           // return Json(modules); постман
        }

        [HttpGet]
        public async Task<IActionResult> DeleteModule(int Id)
        {
            var module = await _context.Modules
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == Id);

            if (module == null)
            {
                return NotFound($"Модуль з ID {Id} не знайдено.");
            }

            return View(module);
        }

        [HttpPost, ActionName("DeleteModule")]
        public async Task<IActionResult> DeleteModuleConfirmed(int Id)
        {
            var module = await _context.Modules
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == Id);

            if (module != null)
            {
                foreach (var lesson in module.Lessons)
                {
                    foreach (var resource in lesson.Resources)
                    {
                        var publicId = GetPublicIdFromUrl(resource.FilePath);
                        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    }
                }

                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("GetModules");
        }

        [HttpGet]
        public async Task<IActionResult> UpdateModule(int moduleId)
        {
            var module = await _context.Modules
                .Include(m=>m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == moduleId);
            if (module == null) {
                return NotFound($"Модуль з ID {moduleId} не знайдено.");
            }
            var updateModule = new UpdateModuleDTO
            {
                Title = module.Title,
                Description = module.Description,
                OrderIndex = module.OrderIndex,
                LessonNames = module.Lessons.Select(l=>l.Title).ToList()

            };
            return View(updateModule);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateModule(UpdateModuleDTO updateModuleDTO)
        {
            if (!ModelState.IsValid) return View(updateModuleDTO);
            var module = await _context.Modules
               .Include(m => m.Lessons)
               .FirstOrDefaultAsync(m => m.Id == updateModuleDTO.Id);

            if (module == null) {
                return NotFound($"Модуль з ID {updateModuleDTO.Id} не знайдено.");
            }
            updateModuleDTO.Title = module.Title;
            updateModuleDTO.OrderIndex = module.OrderIndex;
            updateModuleDTO.Description = module.Description;
            
            await _context.SaveChangesAsync();
            return RedirectToAction("GetModules");
        }

        private string GetPublicIdFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                // Отримуємо останній сегмент шляху, наприклад "v12345/public_id.jpg"
                var segments = uri.Segments;
                var fileNameWithExtension = segments.Last();

                // Видаляємо розширення (.jpg, .png тощо), щоб отримати чистий PublicId
                var publicId = Path.GetFileNameWithoutExtension(fileNameWithExtension);

                return publicId;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public async Task<int> GetModulesNumber()
        {
            var moduleNum = await _context.Modules.CountAsync();
            return moduleNum;
        }
        public async Task<int> GetLessonsNumber()
        {
            var lessonNum = await _context.Lessons.CountAsync();
            return lessonNum;
        }
    }
}
