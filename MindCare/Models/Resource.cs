using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MindCare.Models;

public class Resource
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Content { get; set; }

    [StringLength(2048)]
    [Url]
    public string? Url { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CategoryId { get; set; }
    public ResourceCategory? ResourceCategory { get; set; }
    [StringLength(450)] public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    [StringLength(30)] public string CreatedByRole { get; set; } = "System";
    public ResourceStatus Status { get; set; } = ResourceStatus.Published;
    public bool IsFeatured { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [StringLength(450)] public string? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }
    [StringLength(1000)] public string? ReviewNote { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public int ViewCount { get; set; }
    public ICollection<ResourceBookmark> Bookmarks { get; set; } = new List<ResourceBookmark>();

    public static bool IsSafeExternalUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
