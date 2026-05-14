using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace diplomaProject.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Lesson Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Content")]
        public string Content { get; set; }
        public string? Description { get; set; }

        [Display(Name = "Video URL")]
        public string? VideoUrl { get; set; }

        public Module Module { get; set; }
        public int ModuleId { get; set; }

        [Display(Name = "Resources")]
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
        public ICollection<Homework> Homeworks { get; set; }

        public int LessonIndex { get; set; }

        // --- НОВЫЕ ПОЛЯ ДЛЯ АДМИНКИ ---
        [NotMapped]
        public int ModuleIndex { get; set; } // Для вывода номера модуля в таблице

        [NotMapped]
        public int UserCompletedNum { get; set; } // Статистика прохождений
        // ------------------------------

        //public string GetBackgroundImage()
        //{
        //    ... (твой закомментированный код сохранен)
        //}

        [NotMapped]
        public string? FirstImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(Content)) return null;
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(Content))
                    {
                        var blocks = doc.RootElement.GetProperty("blocks");
                        foreach (var block in blocks.EnumerateArray())
                        {
                            if (block.GetProperty("type").GetString() == "image")
                            {
                                return block.GetProperty("data").GetProperty("file").GetProperty("url").GetString();
                            }
                        }
                    }
                }
                catch { return null; }
                return null;
            }
        }

        [NotMapped]
        public List<(string Text, int Level)> TableOfContents
        {
            get
            {
                var toc = new List<(string Text, int Level)>();
                if (string.IsNullOrEmpty(Content)) return toc;
                try
                {
                    using (JsonDocument doc = JsonDocument.Parse(Content))
                    {
                        var blocks = doc.RootElement.GetProperty("blocks");
                        foreach (var block in blocks.EnumerateArray())
                        {
                            if (block.GetProperty("type").GetString() == "header")
                            {
                                var text = block.GetProperty("data").GetProperty("text").GetString();
                                var level = block.GetProperty("data").GetProperty("level").GetInt32();
                                if (!string.IsNullOrEmpty(text)) { toc.Add((text, level)); }
                            }
                        }
                    }
                }
                catch { }
                return toc;
            }
        }
    }
}