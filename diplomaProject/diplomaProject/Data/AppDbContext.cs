using diplomaProject.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace diplomaProject.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext()
        {

        }
        public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
        {

        }
     
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseRegistration> CourseRegistrations { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserProgress> UserProgresses { get; set; }
        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<HomeworkSubmission> HomeworkSubmissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //  викликає внутрішні налаштування Identity для створення таблиць AspNetUsers, AspNetRoles тощо.
            base.OnModelCreating(builder);

            builder.Entity<UserProgress>()
        .HasOne(p => p.Module)
        .WithMany()
        .HasForeignKey(p => p.ModuleId)
        .OnDelete(DeleteBehavior.Restrict); // Або NoAction

            // Вимикаємо каскадне видалення для Лекцій у таблиці прогресу
            builder.Entity<UserProgress>()
                .HasOne(p => p.Lesson)
                .WithMany()
                .HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Якщо виникне помилка з UserId, можна додати і це:
            builder.Entity<UserProgress>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
           


        }
    }
}
