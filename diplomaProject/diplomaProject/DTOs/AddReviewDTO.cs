using diplomaProject.Models;

namespace diplomaProject.DTOs
{
    public class AddReviewDTO
    {
        public string Content { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
    }
}
