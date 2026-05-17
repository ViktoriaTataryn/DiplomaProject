namespace diplomaProject.DTOs;

public class UserProfileDto
{
    public DateTime RegistrationDate { get; set; }
    public EditUserDto? EditModel { get; set; }
    public bool IsPaid { get; set; }
    public DateTime LastActivity { get; set; }
    public bool IsActive { get; set; }
    public DateTime? PaymentDate { get; set; }
}