using System.ComponentModel.DataAnnotations;

namespace diplomaProject.Models;

public class Question
{
    [Key] public int Id { get; set; }

    [Required] public required string Text { get; set; } // Текст самого вопроса

    // Значение True, если несколько ответов могут быть правильными (флажки)
    // Значение False, если только один ответ правильный (переключатели)
    public bool IsMultipleChoice { get; set; }

    // Ссылка на урок, к которому относится этот вопрос
    //public int LessonId { get; set; }
    public int HomeworkId { get; set; }
    public Homework? Homework { get; set; }

    // Свойство навигации Entity Framework для связи параметров
    public List<AnswerOption> Options { get; set; } = new();
}