using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using MindCare.Models;
using MindCare.Services;
using MindCare.ViewModels;
using System.Text;

namespace MindCare.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleRedirectService _roleRedirectService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleRedirectService roleRedirectService,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleRedirectService = roleRedirectService;
        _emailService = emailService;
        _logger = logger;
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
                if (await RequiresCounsellorPasswordChangeAsync(user))
                {
                    return RedirectToAction("ChangeInitialPassword", "Counsellor");
                }

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

    [HttpGet]
    public IActionResult ForgotPassword() => UnifiedForgotPasswordView();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return UnifiedForgotPasswordView(model);

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            var isCounsellor = await _userManager.IsInRoleAsync(user, RoleNames.Counsellor);
            var isNormalUser = await IsNormalUserAsync(user);
            if (isCounsellor || isNormalUser)
            {
                try
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                    var resetAction = isCounsellor ? nameof(ResetPassword) : nameof(UserResetPassword);
                    var link = Url.Action(resetAction, "Account", new { email = user.Email, token = encodedToken }, Request.Scheme);
                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        var body = $"<p>A password reset was requested for your MindCare account.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Reset Password</a></p><p>If you did not request this, you can ignore this email.</p>";
                        await _emailService.SendAsync(user.Email!, "Reset your MindCare password", body);
                    }
                }
                catch (Exception exception)
                {
                    // Never include reset tokens, passwords, email addresses, or reset URLs in logs.
                    _logger.LogError(exception, "Unable to send a password-reset email.");
                }
            }
        }

        TempData["SuccessMessage"] = "If an eligible account exists for that email, password-reset instructions have been sent.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "This password reset link is invalid or has expired. Please request a new one.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired. Please request a new one.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !await _userManager.IsInRoleAsync(user, RoleNames.Counsellor))
        {
            ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired. Please request a new one.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var claims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in claims.Where(claim => claim.Type == SecurityClaimTypes.MustChangePassword))
        {
            await _userManager.RemoveClaimAsync(user, claim);
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == user.Id) await _signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "Your password has been reset. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        return user is null ? Challenge() : !await IsNormalUserAsync(user) ? Forbid() : View(user);
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        var user = await _userManager.GetUserAsync(User);
        return user is null ? Challenge() : !await IsNormalUserAsync(user) ? Forbid() : View(new UserChangePasswordViewModel());
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(UserChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!await IsNormalUserAsync(user)) return Forbid();
        if (!ModelState.IsValid) return View(model);

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["SuccessMessage"] = "Your password has been changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult UserResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "This password reset link is invalid or has expired. Please request a new one.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return UserResetPasswordView(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return UserResetPasswordView(model);

        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token)); }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired. Please request a new one.");
            return UserResetPasswordView(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !await IsNormalUserAsync(user))
        {
            ModelState.AddModelError(string.Empty, "This password reset link is invalid or has expired. Please request a new one.");
            return UserResetPasswordView(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            var message = result.Errors.Any(error => error.Code == "InvalidToken")
                ? "This password reset link is invalid or has expired. Please request a new one."
                : "We couldn't reset your password. Please choose a password that meets the password requirements.";
            ModelState.AddModelError(string.Empty, message);
            return UserResetPasswordView(model);
        }

        return RedirectToAction(nameof(UserResetPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult UserResetPasswordConfirmation() => View();

    [Authorize(Roles = RoleNames.User)]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!await IsNormalUserAsync(user)) return Forbid();

        return View(new EditUserProfileViewModel
        {
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber
        });
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditUserProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!await IsNormalUserAsync(user)) return Forbid();
        if (!ModelState.IsValid) return View(model);

        var email = model.Email.Trim();
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var existingEmailUser = await _userManager.FindByEmailAsync(email);
            var existingUserName = await _userManager.FindByNameAsync(email);
            if ((existingEmailUser is not null && existingEmailUser.Id != user.Id) ||
                (existingUserName is not null && existingUserName.Id != user.Id))
            {
                ModelState.AddModelError(nameof(model.Email), "This email address is already in use.");
                return View(model);
            }

            user.Email = email;
            user.NormalizedEmail = _userManager.NormalizeEmail(email);
            user.UserName = email;
            user.NormalizedUserName = _userManager.NormalizeName(email);
        }

        user.Name = model.Name.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["SuccessMessage"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
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

    private async Task<bool> RequiresCounsellorPasswordChangeAsync(ApplicationUser user)
    {
        if (!await _userManager.IsInRoleAsync(user, RoleNames.Counsellor)) return false;
        var claims = await _userManager.GetClaimsAsync(user);
        return claims.Any(claim => claim.Type == SecurityClaimTypes.MustChangePassword && claim.Value == SecurityClaimTypes.True);
    }

    private async Task<bool> IsNormalUserAsync(ApplicationUser user) =>
        await _userManager.IsInRoleAsync(user, RoleNames.User) &&
        !await _userManager.IsInRoleAsync(user, RoleNames.Counsellor) &&
        !await _userManager.IsInRoleAsync(user, RoleNames.Admin);

    private ViewResult UnifiedForgotPasswordView(ForgotPasswordViewModel? model = null)
    {
        ViewData["AccountLabel"] = "Account recovery";
        ViewData["ForgotPasswordAction"] = nameof(ForgotPassword);
        ViewData["ForgotPasswordDescription"] = "Enter the email address associated with your MindCare account. If an eligible account exists, we’ll send password-reset instructions.";
        return View("ForgotPassword", model);
    }

    private ViewResult UserResetPasswordView(ResetPasswordViewModel model)
    {
        ViewData["AccountLabel"] = "Account security";
        ViewData["ResetPasswordAction"] = nameof(UserResetPassword);
        ViewData["ResetPasswordTitle"] = "Set a New Password";
        ViewData["ResetPasswordText"] = "Choose a new password for your MindCare account.";
        ViewData["ResetCancelAction"] = nameof(ForgotPassword);
        return View("ResetPassword", model);
    }
}
