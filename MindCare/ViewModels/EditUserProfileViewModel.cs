using System.ComponentModel.DataAnnotations;

namespace MindCare.ViewModels;

public class EditUserProfileViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }
}
