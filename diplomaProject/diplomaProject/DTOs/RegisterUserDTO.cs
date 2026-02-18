using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs
{
    public class RegisterUserDTO
    {
        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Прізвище є обов'язковим")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Некоректний формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролі не збігаються")]
        public string ConfirmPassword { get; set; }
    }
}
