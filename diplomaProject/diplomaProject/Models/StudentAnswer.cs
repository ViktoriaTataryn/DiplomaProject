namespace diplomaProject.Models;

public class StudentAnswer
{
    public int Id { get; set; }
    public int HomeworkSubmissionId { get; set; } // Зв'язок зі здачею
    public HomeworkSubmission? HomeworkSubmission { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public int SelectedOptionId { get; set; } // Що саме обрав користувач
    public AnswerOption? SelectedOption { get; set; }
}