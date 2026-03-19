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
using System.Text.RegularExpressions;

namespace diplomaProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ProgressService _progressService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(AppDbContext context, ProgressService progressService, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _progressService = progressService;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
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

        [HttpGet]
        public async Task<IActionResult> GetLessons(string searchTerm)
        {
            var lessonQuery = _context.Lessons.Include(l=>l.Module).AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                lessonQuery = lessonQuery.Where(l => l.Title.ToLower().Contains(searchTerm));
            }

            var lessons = await lessonQuery
                .OrderBy(l => l.Module.OrderIndex)
                .ThenBy(l => l.Id)
              .Select(l => new LessonDTO
              {
                  Id = l.Id,
                  Title = l.Title,
                  ModuleIndex= l.Module.OrderIndex,
                  UserCompletedNum = _context.UserProgresses.Count(u => u.LessonId == l.Id && u.Status == ProgressStatus.Completed),
              })
              .ToListAsync();

            ViewBag.CurrentSearch = searchTerm;

            return View(lessons);

             //return Json(lessons); //постман
        }

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
                ModuleId = createLessonDTO.ModuleId,
                Resources = new List<Resource>()
            };

            // Робота з файлами
            if (createLessonDTO.ResourceFiles != null && createLessonDTO.ResourceFiles.Count > 0)
            {
                // Вказуємо шлях до wwwroot/resources
                var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "resources");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                foreach (var file in createLessonDTO.ResourceFiles)
                {
                    // Генеруємо унікальне ім'я для запобігання дублікатів
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var fullPath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Зберігаємо шлях у базу (відносний шлях для відображення в браузері)
                    lesson.Resources.Add(new Resource
                    {
                        FileName = file.FileName,
                        FilePath = "/resources/" + fileName 
                    });
                }
            }

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetLessons");
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile upload)
        {
            if (upload == null || upload.Length == 0) return BadRequest();

            // 1. Шлях до папки wwwroot/resources
            var folderPath = Path.Combine(_webHostEnvironment.WebRootPath, "resources");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // 2. Генеруємо унікальне ім'я
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(upload.FileName);
            var fullPath = Path.Combine(folderPath, fileName);

            // 3. Зберігаємо на диск
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await upload.CopyToAsync(stream);
            }

            // 4. Формуємо пряме посилання для редактора
            var url = $"/resources/{fileName}";

            // CKEditor очікує відповідь саме в такому форматі JSON:
            return Json(new
            {
                uploaded = true,
                url = "/resources/" + fileName
            });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteLesson(int lessonId)
        {
            var lesson = await _context.Lessons
               .Include(l => l.Resources)
               .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return NotFound($"Лекцію з ID {lessonId} не знайдено.");
            }

            return View(lesson);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLessonConfirmed(int lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null) return NotFound();

            // --- А. ВИДАЛЕННЯ КАРТИНОК З ТЕКСТУ (Regex) ---
            var content = lesson.Content;
            if (!string.IsNullOrEmpty(content))
            {
                var imgTags = Regex.Matches(content, "<img.+?src=[\"'](.+?)[\"'].*?>");
                foreach (Match match in imgTags)
                {
                    var url = match.Groups[1].Value;
                    // Обрізаємо можливі параметри запиту, якщо вони є (наприклад, ?v=123)
                    var cleanUrl = url.Split('?')[0].TrimStart('/');
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, cleanUrl);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }

            // 2. Видаляємо фізичні файли з папки wwwroot/resources
            if (lesson.Resources != null && lesson.Resources.Any())
            {
                foreach (var resource in lesson.Resources)
                {
                    // Формуємо повний шлях до файлу на сервері
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, resource.FilePath.TrimStart('/'));

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath); // Видаляємо файл із диска
                    }
                    _context.Resources.Remove(resource);
                }
            }

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction("GetLessons");
        }

        [HttpGet]
        public async Task<IActionResult> UpdateLesson(int lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Resources)
                .FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null)
            {
                return NotFound($"Лекцію з id {lessonId} не знайдено");
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
            return View(updateLesson);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLesson(UpdateLessonDTO updateLessonDTO)
        {
            if (!ModelState.IsValid)
            {
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
            lesson.ModuleId=updateLessonDTO.ModuleId;
            

            await _context.SaveChangesAsync();
            return RedirectToAction("GetLessons");

        }

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
        public async Task<IActionResult> DeleteModule(int moduleId)
        {
            var module =await _context.Modules
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == moduleId); 

            if (module == null) { 
               return NotFound($"Модуль з ID {moduleId} не знайдено.");
            }
            
            return View(module);
        }
        
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int moduleId)
        {
            var module = await _context.Modules
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (module != null)
            {
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
