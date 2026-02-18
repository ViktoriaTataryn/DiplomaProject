using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs
{
    public class LoginUserDTO
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress(ErrorMessage = "Некоректний формат Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введіть пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
