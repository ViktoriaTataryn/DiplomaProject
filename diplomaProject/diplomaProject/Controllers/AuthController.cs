using diplomaProject.DTOs;
using diplomaProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace diplomaProject.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Student");

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // return RedirectToAction("Index", "Home");
                    return Ok(new { message = "Реєстрація успішна!" });
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

                //return BadRequest("Невірний логін або пароль.");

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Невірний логін або пароль.");
            }

            //return BadRequest(ModelState);
            return View(loginUserDTO);
        }
    }
}
