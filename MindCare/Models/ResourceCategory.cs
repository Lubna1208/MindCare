using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class ResourceCategory
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(300)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
