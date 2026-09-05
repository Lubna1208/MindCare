using System.ComponentModel.DataAnnotations;
using MindCare.Models;

namespace MindCare.ViewModels;

public class CreateForumPostViewModel
{
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    public string Content { get; set; } = string.Empty;
}

public class CreateForumCommentViewModel
{
    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;
}

public class CreateForumReportViewModel
{
    public int? PostId { get; set; }

    public int? CommentId { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ForumPostDetailsViewModel
{
    public ForumPost Post { get; set; } = null!;

    public CreateForumCommentViewModel CommentForm { get; set; } = new();

    public CreateForumReportViewModel ReportForm { get; set; } = new();
}
