namespace diplomaProject.DTOs
{
    public class QuestionDTO
    {
        public string Text { get; set; }
        public int CorrectAnswerIndex { get; set; }
        public List<AnswerOptionDTO> Answers { get; set; }
    }
    public class AnswerOptionDTO
    {
        public string Text { get; set; }
    }
}
