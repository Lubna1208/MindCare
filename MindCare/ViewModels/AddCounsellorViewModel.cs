using System.ComponentModel.DataAnnotations;

namespace MindCare.ViewModels;

public class AddCounsellorViewModel
{
    [Required]
    [Display(Name = "Full Name")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(254, ErrorMessage = "Email must be 254 characters or fewer.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 64 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$", ErrorMessage = "Password must include uppercase, lowercase, number, and special character.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Phone number must contain 10 to 15 digits only.")]
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
