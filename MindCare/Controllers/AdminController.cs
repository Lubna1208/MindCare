using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> ForumReports()
    {
        var reports = await _context.ForumReports
            .Include(report => report.Post)
            .Include(report => report.Comment)
                .ThenInclude(comment => comment!.ForumPost)
            .Where(report => report.Status == ForumReportStatuses.Pending)
            .OrderBy(report => report.CreatedAt)
            .ToListAsync();

        return View(reports);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReportedPost(int reportId)
    {
        var report = await _context.ForumReports
            .Include(item => item.Post)
            .FirstOrDefaultAsync(item => item.Id == reportId);

        if (report is null)
        {
            return NotFound();
        }

        if (report.Post is not null)
        {
            await ClearReportReferencesForPostAsync(report.Post.Id);
            _context.ForumPosts.Remove(report.Post);
        }

        report.Status = ForumReportStatuses.Resolved;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Reported post removed.";
        return RedirectToAction(nameof(ForumReports));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReportedComment(int reportId)
    {
        var report = await _context.ForumReports
            .Include(item => item.Comment)
            .FirstOrDefaultAsync(item => item.Id == reportId);

        if (report is null)
        {
            return NotFound();
        }

        if (report.Comment is not null)
        {
            await ClearReportReferencesForCommentAsync(report.Comment.Id);
            _context.ForumComments.Remove(report.Comment);
        }

        report.Status = ForumReportStatuses.Resolved;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Reported comment removed.";
        return RedirectToAction(nameof(ForumReports));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveForumReport(int reportId)
    {
        var report = await _context.ForumReports.FirstOrDefaultAsync(item => item.Id == reportId);
        if (report is null)
        {
            return NotFound();
        }

        report.Status = ForumReportStatuses.Reviewed;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Report dismissed.";
        return RedirectToAction(nameof(ForumReports));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReportedUserAccount(int reportId)
    {
        var report = await _context.ForumReports
            .Include(item => item.Post)
            .Include(item => item.Comment)
            .FirstOrDefaultAsync(item => item.Id == reportId);

        if (report is null)
        {
            return NotFound();
        }

        var reportedUserId = report.Post?.UserId ?? report.Comment?.UserId;
        if (reportedUserId is null)
        {
            TempData["ErrorMessage"] = "The reported content is no longer available.";
            return RedirectToAction(nameof(ForumReports));
        }

        var reportedUser = await _userManager.FindByIdAsync(reportedUserId);
        if (reportedUser is null)
        {
            TempData["ErrorMessage"] = "The reported user account was not found.";
            return RedirectToAction(nameof(ForumReports));
        }

        if (await _userManager.IsInRoleAsync(reportedUser, RoleNames.Admin))
        {
            TempData["ErrorMessage"] = "Admin accounts cannot be deleted from forum moderation.";
            return RedirectToAction(nameof(ForumReports));
        }

        var blockingDependencyMessage = await GetUserDeletionBlockingDependencyMessageAsync(reportedUserId);
        if (blockingDependencyMessage is not null)
        {
            TempData["ErrorMessage"] = blockingDependencyMessage;
            return RedirectToAction(nameof(ForumReports));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var postIds = await _context.ForumPosts
                .Where(post => post.UserId == reportedUserId)
                .Select(post => post.Id)
                .ToListAsync();
            foreach (var postId in postIds)
            {
                await ClearReportReferencesForPostAsync(postId);
            }

            var commentIds = await _context.ForumComments
                .Where(comment => comment.UserId == reportedUserId)
                .Select(comment => comment.Id)
                .ToListAsync();
            foreach (var commentId in commentIds)
            {
                await ClearReportReferencesForCommentAsync(commentId);
            }

            var comments = await _context.ForumComments
                .Where(comment => comment.UserId == reportedUserId)
                .ToListAsync();
            var posts = await _context.ForumPosts
                .Where(post => post.UserId == reportedUserId)
                .ToListAsync();

            _context.ForumComments.RemoveRange(comments);
            _context.ForumPosts.RemoveRange(posts);
            report.Status = ForumReportStatuses.Resolved;
            await _context.SaveChangesAsync();

            var deleteResult = await _userManager.DeleteAsync(reportedUser);
            if (!deleteResult.Succeeded)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Reported user account could not be deleted.";
                return RedirectToAction(nameof(ForumReports));
            }

            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Reported user account could not be safely deleted because related records still reference it.";
            return RedirectToAction(nameof(ForumReports));
        }

        TempData["SuccessMessage"] = "Reported user account deleted.";
        return RedirectToAction(nameof(ForumReports));
    }

    [HttpGet]
    public IActionResult AddCounsellor()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCounsellor(AddCounsellorViewModel model)
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
            Name = model.FullName.Trim(),
            UserName = email,
            Email = email,
            PhoneNumber = model.Phone.Trim(),
            EmailConfirmed = true
        };

        IdentityResult? createResult = null;
        IdentityResult? roleResult = null;
        IdentityResult? claimResult = null;

        try
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded) return;

                roleResult = await _userManager.AddToRoleAsync(user, RoleNames.Counsellor);
                if (!roleResult.Succeeded) return;

                claimResult = await _userManager.AddClaimAsync(
                    user,
                    new Claim(SecurityClaimTypes.MustChangePassword, SecurityClaimTypes.True));
                if (!claimResult.Succeeded) return;

                _context.CounsellorProfiles.Add(new CounsellorProfile
                {
                    ApplicationUserId = user.Id,
                    Phone = model.Phone.Trim(),
                    Specialization = model.Specialization.Trim(),
                    Qualification = model.Qualification.Trim(),
                    Experience = model.Experience.Trim()
                });
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The counsellor account could not be created. Please try again.");
            return View(model);
        }

        if (createResult is null || !createResult.Succeeded)
        {
            foreach (var error in createResult?.Errors ?? []) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        if (roleResult is null || !roleResult.Succeeded || claimResult is null || !claimResult.Succeeded)
        {
            foreach (var error in (roleResult?.Errors ?? []).Concat(claimResult?.Errors ?? [])) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["SuccessMessage"] = "Counsellor account created with a temporary password.";
        return RedirectToAction(nameof(Counsellors));
    }

    public async Task<IActionResult> Counsellors()
    {
        var counsellors = await _context.CounsellorProfiles
            .Include(profile => profile.ApplicationUser)
            .OrderBy(profile => profile.ApplicationUser.Name)
            .ToListAsync();

        return View(counsellors);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCounsellor(int id)
    {
        var profile = await _context.CounsellorProfiles
            .Include(profile => profile.ApplicationUser)
            .FirstOrDefaultAsync(profile => profile.Id == id);

        if (profile is null)
        {
            TempData["ErrorMessage"] = "Counsellor was not found.";
            return RedirectToAction(nameof(Counsellors));
        }

        var hasAppointments = await _context.Appointments
            .AnyAsync(appointment => appointment.CounsellorProfileId == profile.Id);
        if (hasAppointments)
        {
            TempData["ErrorMessage"] = "This counsellor cannot be deleted because related appointments exist.";
            return RedirectToAction(nameof(Counsellors));
        }

        var user = profile.ApplicationUser;
        var hasMessages = await _context.Messages
            .AnyAsync(message => message.SenderUserId == user.Id || message.ReceiverUserId == user.Id);
        if (hasMessages)
        {
            TempData["ErrorMessage"] = "This counsellor cannot be deleted because related appointment messages exist.";
            return RedirectToAction(nameof(Counsellors));
        }

        try
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            IdentityResult? deleteResult = null;

            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                _context.CounsellorProfiles.Remove(profile);
                await _context.SaveChangesAsync();

                deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return;
                }

                await transaction.CommitAsync();
            });

            if (deleteResult is null || !deleteResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Counsellor account could not be deleted.";
                return RedirectToAction(nameof(Counsellors));
            }
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "This counsellor could not be deleted because related records still exist.";
            return RedirectToAction(nameof(Counsellors));
        }

        TempData["SuccessMessage"] = "Counsellor deleted successfully.";
        return RedirectToAction(nameof(Counsellors));
    }

    private async Task ClearReportReferencesForPostAsync(int postId)
    {
        var commentIds = await _context.ForumComments
            .Where(comment => comment.ForumPostId == postId)
            .Select(comment => comment.Id)
            .ToListAsync();

        var reports = await _context.ForumReports
            .Where(report => report.PostId == postId || report.CommentId != null && commentIds.Contains(report.CommentId.Value))
            .ToListAsync();

        foreach (var report in reports)
        {
            if (report.PostId == postId)
            {
                report.PostId = null;
            }

            if (report.CommentId is not null && commentIds.Contains(report.CommentId.Value))
            {
                report.CommentId = null;
            }
        }
    }

    private async Task ClearReportReferencesForCommentAsync(int commentId)
    {
        var reports = await _context.ForumReports
            .Where(report => report.CommentId == commentId)
            .ToListAsync();

        foreach (var report in reports)
        {
            report.CommentId = null;
        }
    }

    private async Task<string?> GetUserDeletionBlockingDependencyMessageAsync(string userId)
    {
        if (await _context.CounsellorProfiles.AnyAsync(profile => profile.ApplicationUserId == userId))
        {
            return "Reported user account could not be safely deleted because it is linked to a counsellor profile.";
        }

        if (await _context.Appointments.AnyAsync(appointment => appointment.UserId == userId))
        {
            return "Reported user account could not be safely deleted because it is linked to appointments.";
        }

        if (await _context.Messages.AnyAsync(message => message.SenderUserId == userId || message.ReceiverUserId == userId))
        {
            return "Reported user account could not be safely deleted because it is linked to appointment messages.";
        }

        return null;
    }
}
