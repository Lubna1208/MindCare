using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class ForumComment
{
    public int Id { get; set; }

    public int ForumPostId { get; set; }

    public ForumPost ForumPost { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public ICollection<ForumReport> Reports { get; set; } = new List<ForumReport>();
}
