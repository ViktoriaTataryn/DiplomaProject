using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models;

public class RegisterViewModel
{
    // Added first name field
    [Required]
    [Display(Name = "First Name")]
    public required string FirstName { get; set; }

    // Added last name field
    [Required]
    [Display(Name = "Last Name")]
    public required string LastName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public required string Email { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public required string Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public required string ConfirmPassword { get; set; }
}