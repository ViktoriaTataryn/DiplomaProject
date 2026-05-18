using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace diplomaProject.Models;

//класс описывает таблицу пользователя в базе данных
public class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }


    //автоматическая дата регистрации (текущее время)   
    [Display(Name = "Registration Date")] public DateTime RegistrationDate { get; set; } = DateTime.Now;
}