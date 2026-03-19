using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                RegistrationDate = DateTime.Now,
                FirstName = model.FirstName, // Gets value from the form
                LastName = model.LastName    // Gets value from the form
                // Примітка: FirstName та LastName тепер отримуються безпосередньо з моделі
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var registration = new CourseRegistration
                {
                    UserId = user.Id,
                    CourseId = model.CourseId,
                    RegisterAt = DateTime.Now
                };
                _context.CourseRegistrations.Add(registration);

                var roleResult = await _userManager.AddToRoleAsync(user, "Student");

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // 2. Створюємо посилання (Callback URL)
                var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                    new { userId = user.Id, token = token }, Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email, "Підтвердження реєстрації",
                    $"Будь ласка, підтвердіть вашу реєстрацію, перейшовши за посиланням: <a href='{confirmationLink}'>ПІДТВЕРДИТИ</a>");

                //return Ok(new { message = "Лист для підтвердження надіслано на вашу пошту." }); //тест для постман

                return View("RegisterSuccess");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // model.CourseId збережеться автоматично, бо він є в моделі
            return View(model);

            //if (!result.Succeeded)
            //{
            //    foreach (var error in result.Errors)
            //    {
            //        ModelState.AddModelError(string.Empty, error.Description);
            //    }
            //    return View(model);
            //}

            // var roleResult = await _userManager.AddToRoleAsync(user, "Student");
            //if (!roleResult.Succeeded)
            //{
            //    var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
            //    throw new Exception($"ПОМИЛКА ДОДАВАННЯ РОЛІ: {errors}");
            //}

            //var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            //// 2. Створюємо посилання (Callback URL)
            //var confirmationLink = Url.Action("ConfirmEmail", "Auth",
            //    new { userId = user.Id, token = token }, Request.Scheme);

            //await _emailSender.SendEmailAsync(user.Email, "Підтвердження реєстрації",
            //    $"Будь ласка, підтвердіть вашу реєстрацію, перейшовши за посиланням: <a href='{confirmationLink}'>ПІДТВЕРДИТИ</a>");

            ////return Ok(new { message = "Лист для підтвердження надіслано на вашу пошту." }); //тест для постман

            //return View("RegisterSuccess");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return BadRequest("Некоректні дані");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("Користувача не знайдено");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // return Ok("Пошту успішно підтверджено! Тепер ви можете увійти."); //тест для постман
                TempData["StatusMessage"] = "Пошту підтверджено. Тепер ви можете увійти.";
                return RedirectToAction("Login");
            }
            return BadRequest("Помилка підтвердження");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Check if email is confirmed before signing in
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError(string.Empty, "Вам потрібно підтвердити електронну пошту перед входом. Перевірте свою скриньку.");
                    return View(model);
                }

                // Enabled brute-force protection
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    isPersistent: model.RememberMe,
                    lockoutOnFailure: true);

                //if (result.Succeeded)
                //{
                //    return Ok(new { message = "Вхід успішний!" });
                //}
                //return BadRequest("Невірний логін або пароль."); //тест для постман

                if (result.Succeeded)
                {
                    // Направляємо користувача на список курсів після входу
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Course");
                }

                // Handle Account Lockout
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Акаунт заблоковано через завелику кількість невдалих спроб входу. Спробуйте пізніше.");
                    return View(model);
                }

                // Handle Unallowed Login
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "Вхід заборонено. Перевірте, чи підтверджена ваша пошта.");
                    return View(model);
                }

                ModelState.AddModelError(string.Empty, "Невірний логін або пароль.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut(string? returnUrl = null) // Fixed CS8625 Warning
        {
            await _signInManager.SignOutAsync();
            if (returnUrl != null && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}