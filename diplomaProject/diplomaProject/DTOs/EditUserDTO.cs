using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs;

public class EditUserDto
{
    [Required] public required string FirstName { get; set; }

    [Required] public required string LastName { get; set; }

    public required string UserEmail { get; set; }

    [Phone(ErrorMessage = "Некоректний формат номера")]
    [RegularExpression(@"^\+?\d{10,13}$", ErrorMessage = "Номер має бути у форматі +380...")]
    public required string UserPhone { get; set; }

    [DataType(DataType.Password)] public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "Пароль має бути не менше 6 символів")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Паролі не збігаються")]
    public string? ConfirmPassword { get; set; }
}