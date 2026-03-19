using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models
{
    public class AnswerOption
    {
        [Key]
        public int Id { get; set; }

        // Внешнтй ключ для привязки этой опции к конкретному вопросу
        public int QuestionId { get; set; }

        [Required]
        public string Text { get; set; } // Текст ответа отображаемый ученику

        // Секретный флаг используется только на стороне сервера!!1 никогда не отправляйте его в представление
        public bool IsCorrect { get; set; }

        // Свойство навигации возвращает нас к вопросу
        public Question Question { get; set; }
    }
}