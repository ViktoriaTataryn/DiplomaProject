using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models;

public class ForgotPasswordViewModel
{
    // Попросьба пользователя ввести адрес электронной почты
    [Required(ErrorMessage = "The Email field is required.")]
    [EmailAddress(ErrorMessage = "Invalid Email Address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}