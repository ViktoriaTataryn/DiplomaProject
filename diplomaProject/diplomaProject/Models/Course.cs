using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    public class Course
    {
        //у каждого курса должен быть уникальный номер (ID)
        [Key]
        public int Id { get; set; }

        // Название курса
        [Required] //название обязательно должно быть
        public string Title { get; set; }
        // Описание курса
        public string Description { get; set; }

        // Дата создания (чтобы знать, когда курс добавили)
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}