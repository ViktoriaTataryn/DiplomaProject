using diplomaProject.Data;
using diplomaProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Controllers;

public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailSender emailSender,
    AppDbContext context)
    : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            RegistrationDate = DateTime.Now,
            FirstName = model.FirstName, // Gets value from the form
            LastName = model.LastName // Gets value from the form
            // Примітка: FirstName та LastName тепер отримуються безпосередньо з моделі
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            var course = await context.Courses.FirstOrDefaultAsync();
            var courseId = course?.Id ?? 0;
            var registration = new CourseRegistration
            {
                UserId = user.Id,
                CourseId = courseId,
                RegisterAt = DateTime.Now
            };
            context.CourseRegistrations.Add(registration);

            await userManager.AddToRoleAsync(user, "Student");

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            // 2. Створюємо посилання (Callback URL)
            var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                new { userId = user.Id, token }, Request.Scheme);

            await emailSender.SendEmailAsync(user.Email, "Підтвердження реєстрації",
                $"Будь ласка, підтвердіть вашу реєстрацію, перейшовши за посиланням: <a href='{confirmationLink}'>ПІДТВЕРДИТИ</a>");

            //return Ok(new { message = "Лист для підтвердження надіслано на вашу пошту." }); //тест для постман

            return View("RegisterSuccess");
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);

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
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("Користувача не знайдено");

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) return BadRequest("Помилка підтвердження");
        // return Ok("Пошту успішно підтверджено! Тепер ви можете увійти."); //тест для постман
        TempData["StatusMessage"] = "Пошту підтверджено. Тепер ви можете увійти.";
        return RedirectToAction("Login");
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
        if (!ModelState.IsValid) return View(model);
        // Check if email is confirmed before signing in
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Користувача не знайдено");
            return View(model);
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            ModelState.AddModelError(string.Empty,
                "Вам потрібно підтвердити електронну пошту перед входом. Перевірте свою скриньку.");
            return View(model);
        }

        // Enabled brute-force protection
        var result = await signInManager.PasswordSignInAsync(
            model.Email,
            model.Password,
            model.RememberMe,
            true);

        //if (result.Succeeded)
        //  return Ok(new { message = "Вхід успішний!" });
        //return BadRequest("Невірний логін або пароль."); //тест для постман

        if (result.Succeeded)
        {
            // Направляємо користувача на список курсів після входу
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            //return RedirectToAction("Index", "Course");
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        // Handle Account Lockout
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Акаунт заблоковано через завелику кількість невдалих спроб входу. Спробуйте пізніше.");
            return View(model);
        }

        // Handle Unallowed Login
        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "Вхід заборонено. Перевірте, чи підтверджена ваша пошта.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Невірний логін або пароль.");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOut(string? returnUrl = null) // Fixed CS8625 Warning
    {
        await signInManager.SignOutAsync();
        if (returnUrl != null && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

        return RedirectToAction("Login");
    }

    // --- НОВІ МЕТОДИ ДЛЯ ВІДНОВЛЕННЯ ПАРОЛЯ ---

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null || !await userManager.IsEmailConfirmedAsync(user))
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToAction(nameof(ForgotPasswordConfirmation));

            // Generate password reset token
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Create callback link
            var callbackUrl = Url.Action("ResetPassword", "Auth",
                new { email = user.Email, token }, Request.Scheme);

            // Send email
            await emailSender.SendEmailAsync(model.Email, "Reset Password",
                $"Будь ласка, скиньте ваш пароль, перейшовши за посиланням: <a href='{callbackUrl}'>Скинути пароль</a>");

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    // GET: Displays the reset password form
    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (token == null || email == null)
            // Invalid password reset token or missing email
            return BadRequest("Недійсний токен або email.");

        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return View(model);
    }

    // POST: Processes the reset password request
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded) return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // GET: Confirmation page after successful password reset
    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }
}