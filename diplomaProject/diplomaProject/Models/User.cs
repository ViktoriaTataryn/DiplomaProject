using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    //класс описывает таблицу пользователя в базе данных
    public class User
    {
        //идентификатор пользователя
        [Key]
        public int Id { get; set; }

        // логин пользователя (обязателен для заполнения)
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        // ээлектронная почта с проверкой формата
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        //пароль в открытом или зашифрованном виде
        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; }

        // роль пользователя по умолчанию "студент" или может быть "админ"
        [Display(Name = "User Role")]
        public string Role { get; set; } = "Student";

        //автоматическая дата регистрации (текущее время)
        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}