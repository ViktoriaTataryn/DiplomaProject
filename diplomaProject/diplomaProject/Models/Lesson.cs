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
        public string? VideoUrl { get; set; } // Ссылка на видео, если будет нужно

        // Связь с модулем к которому относится урок
        public Module Module { get; set; }
        public int ModuleId { get; set; }

        // Список всех файлов (картинок, методичек), привязанных к уроку
        [Display(Name = "Resources")]
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
        public ICollection<Homework> Homeworks { get; set; }

        public int LessonIndex { get; set; }

        //public string GetBackgroundImage()
        //{
        //    if (string.IsNullOrEmpty(Content)) return null;

        //    try
        //    {
        //        var data = JsonDocument.Parse(Content);
        //        var firstImage = data.RootElement.GetProperty("blocks")
        //            .EnumerateArray()
        //            .FirstOrDefault(b => b.GetProperty("type").GetString() == "image");

        //        return firstImage.GetProperty("data").GetProperty("file").GetProperty("url").GetString();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        [NotMapped] // Цей атрибут прямо каже Entity Framework: "Не шукай таку колонку в БД"
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
                                // Шлях залежить від вашої конфігурації Image Tool в Editor.js
                                return block.GetProperty("data").GetProperty("file").GetProperty("url").GetString();
                            }
                        }
                    }
                }
                catch
                {
                    return null;
                }
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

                                if (!string.IsNullOrEmpty(text))
                                {
                                    toc.Add((text, level));
                                }
                            }
                        }
                    }
                }
                catch { /* Логування помилки за потреби */ }

                return toc;
            }
        }

    
    }


}

