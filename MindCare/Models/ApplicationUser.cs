using Microsoft.AspNetCore.Identity;

namespace MindCare.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
}
