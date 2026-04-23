namespace diplomaProject.DTOs
{
    public class UserProfileDTO
    {
        public DateTime RegistrationDate { get; set; }
        public EditUserDTO EditModel { get; set; }
        public bool isPaid { get; set; }
        public DateTime LastActivity { get; set; }
        public bool isActive { get; set; }
        public DateTime? PaymentDate { get; set; }

    }
}
