using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class ForumPost
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(3000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();

    public ICollection<ForumReport> Reports { get; set; } = new List<ForumReport>();
}
