using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs;

public class LoginUserDto
{
    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Некоректний формат Email")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Введіть пароль")]
    [DataType(DataType.Password)]
    public required string Password { get; set; }
}