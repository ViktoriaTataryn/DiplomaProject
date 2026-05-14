using System.ComponentModel.DataAnnotations.Schema;

namespace diplomaProject.Models
{
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int OrderIndex { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Course Course { get; set; }
        public int CourseId { get; set; }
        public ICollection<Lesson> Lessons { get; set; }

        [NotMapped]
        public int UserCompletedNum { get; set; } // Сколько студентов завершили модуль

        [NotMapped]
        public int LessonsNum => Lessons?.Count ?? 0; // Количество лекций в модуле
    }
}