using System.Reflection.Metadata;

namespace diplomaProject.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int Rating { get; set; }
    }
}
