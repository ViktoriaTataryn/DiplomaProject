using diplomaProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace diplomaProject.Data
{
    // єто класс, мост между моим кодом и базой данніх
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // регистрация моделей в базе данніх
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<UserProgress> UserProgresses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Review> Reviews { get; set; }
    }
}