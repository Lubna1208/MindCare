using System.ComponentModel.DataAnnotations;
using MindCare.Models;
namespace MindCare.ViewModels;

public class ResourceListViewModel
{
    public IReadOnlyList<Resource> Resources { get; set; } = [];
    public IReadOnlyList<ResourceCategory> Categories { get; set; } = [];
    public string? Search { get; set; } public int? CategoryId { get; set; }
    public string Sort { get; set; } = "featured"; public int Page { get; set; } = 1; public int TotalPages { get; set; }
    public ISet<int> BookmarkedIds { get; set; } = new HashSet<int>();
}
public class ResourceEditViewModel
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string Description { get; set; } = string.Empty;
    [Required] public int? CategoryId { get; set; }
    [Required, MinLength(20)] public string Content { get; set; } = string.Empty;
    [Url, StringLength(2048)] public string? Url { get; set; }
    public bool IsFeatured { get; set; }
    public IReadOnlyList<ResourceCategory> Categories { get; set; } = [];
}
public class ResourceReviewViewModel { public int Id { get; set; } [Required, StringLength(1000)] public string ReviewNote { get; set; } = string.Empty; }
public class ResourceDetailsViewModel { public Resource Resource { get; set; } = null!; public bool IsBookmarked { get; set; } public IReadOnlyList<Resource> Related { get; set; } = []; }
