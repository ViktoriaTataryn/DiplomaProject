using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    //класс описывает таблицу пользователя в базе данных
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

    

        //автоматическая дата регистрации (текущее время)
        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
    }
}