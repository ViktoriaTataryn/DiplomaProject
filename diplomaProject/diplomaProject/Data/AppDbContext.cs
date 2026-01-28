using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {

        }
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {

        }
     
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseRegistration> CourseRegistrations { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LiveMeeting> LiveMeetings { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserProgress> UserProgresses { get; set; }
    }
}
