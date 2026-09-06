using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MindCare.Models;
using MindCare.Services;
using MindCare.ViewModels;

namespace MindCare.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleRedirectService _roleRedirectService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleRedirectService roleRedirectService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleRedirectService = roleRedirectService;
    }

    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
            {
                return await _roleRedirectService.RedirectToDashboardAsync(currentUser, Url);
            }
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            Name = model.Name.Trim(),
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, RoleNames.User);
            await _signInManager.SignInAsync(user, isPersistent: false);
            return await _roleRedirectService.RedirectToDashboardAsync(user, Url);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser is not null)
            {
                return await _roleRedirectService.RedirectToDashboardAsync(currentUser, Url);
            }
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var email = model.Email.Trim();
        var result = await _signInManager.PasswordSignInAsync(
            email,
            model.Password,
            isPersistent: false,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                if (await IsSafeFeatureReturnUrlAsync(user, returnUrl))
                {
                    return LocalRedirect(returnUrl!);
                }

                return await _roleRedirectService.RedirectToDashboardAsync(user, Url);
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        ViewData["ReturnUrl"] = returnUrl;
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<bool> IsSafeFeatureReturnUrlAsync(ApplicationUser user, string? returnUrl)
    {
        if (!Url.IsLocalUrl(returnUrl))
        {
            return false;
        }

        var path = returnUrl!.Split('?', '#')[0];
        if (path.Equals("/Resources", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.Equals("/Mood", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/Assessment", StringComparison.OrdinalIgnoreCase))
        {
            return await _userManager.IsInRoleAsync(user, RoleNames.User);
        }

        return false;
    }
}
