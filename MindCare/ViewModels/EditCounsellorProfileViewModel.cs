using System.ComponentModel.DataAnnotations;

namespace MindCare.ViewModels;

public class EditCounsellorProfileViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Qualification { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Experience { get; set; } = string.Empty;

}
