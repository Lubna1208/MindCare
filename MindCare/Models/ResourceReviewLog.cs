using System.ComponentModel.DataAnnotations;
namespace MindCare.Models;
public class ResourceReviewLog
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public Resource? Resource { get; set; }
    [StringLength(450)] public string AdminUserId { get; set; } = string.Empty;
    [StringLength(30)] public string Action { get; set; } = string.Empty;
    [StringLength(1000)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
