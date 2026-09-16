namespace MindCare.Models;

public class ResourceBookmark
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int ResourceId { get; set; }
    public Resource? Resource { get; set; }
    public DateTime CreatedAt { get; set; }
}
