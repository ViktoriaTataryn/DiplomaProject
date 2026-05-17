using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs;

public class RegisterUserDto
{
    [Required(ErrorMessage = "Ім'я є обов'язковим")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Прізвище є обов'язковим")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Некоректний формат Email")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Пароль є обов'язковим")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Паролі не збігаються")]
    public required string ConfirmPassword { get; set; }
}