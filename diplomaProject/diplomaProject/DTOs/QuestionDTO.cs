using System.ComponentModel.DataAnnotations;

namespace diplomaProject.DTOs;

public class QuestionDto
{
    [Required] public required string Text { get; set; }
    public int CorrectAnswerIndex { get; set; }
    public List<AnswerOptionDto> Answers { get; set; } = [];
}