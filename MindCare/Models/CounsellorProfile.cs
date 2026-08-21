using System.ComponentModel.DataAnnotations;

namespace MindCare.Models;

public class CounsellorProfile
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Qualification { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Experience { get; set; } = string.Empty;
}
