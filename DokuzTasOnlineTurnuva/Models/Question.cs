using System.ComponentModel.DataAnnotations;

namespace DokuzTasOnlineTurnuva.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        public string Option1 { get; set; } = string.Empty;
        
        [Required]
        public string Option2 { get; set; } = string.Empty;
        
        public string? Option3 { get; set; }
        public string? Option4 { get; set; }
        
        [Required]
        public int CorrectAnswer { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
