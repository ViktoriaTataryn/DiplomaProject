using diplomaProject.DTOs;
using diplomaProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace diplomaProject.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender=emailSender;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterUserDTO registerUserDTO)
        {
            if (ModelState.IsValid) {
                var user = new ApplicationUser
                {
                    UserName = registerUserDTO.Email,
                    FirstName = registerUserDTO.FirstName,
                    LastName = registerUserDTO.LastName,
                    Email = registerUserDTO.Email,
                    RegistrationDate = DateTime.Now,
                };
                var result = await _userManager.CreateAsync(user, registerUserDTO.Password);
                await _userManager.AddToRoleAsync(user, "Student");


                if (result.Succeeded)
                {
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
                return BadRequest(result.Errors);
            }
            return View(registerUserDTO);
            
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
        public IActionResult Login()
        {
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginUserDTO loginUserDTO)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    loginUserDTO.Email,
                    loginUserDTO.Password,
                    isPersistent: false,
                    lockoutOnFailure: false);

                //if (result.Succeeded)
                //{
                //    return Ok(new { message = "Вхід успішний!" });
                //}
                //return BadRequest("Невірний логін або пароль."); //тест для постман

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "User");
                }

                ModelState.AddModelError(string.Empty, "Невірний логін або пароль.");
            }

            //return BadRequest(ModelState);
            return View(loginUserDTO);
        }
    }
}
