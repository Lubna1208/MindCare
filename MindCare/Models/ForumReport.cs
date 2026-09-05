using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class ForumReport
{
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string ReporterUserId { get; set; } = string.Empty;

    public int? PostId { get; set; }

    public ForumPost? Post { get; set; }

    public int? CommentId { get; set; }

    public ForumComment? Comment { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = ForumReportStatuses.Pending;
}
