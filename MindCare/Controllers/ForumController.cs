using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User)]
public class ForumController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ForumController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var posts = await _context.ForumPosts
            .Include(post => post.Comments.OrderBy(comment => comment.CreatedAt))
            .OrderByDescending(post => post.CreatedAt)
            .ToListAsync();

        return View(posts);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateForumPostViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateForumPostViewModel model)
    {
        model.Title = (model.Title ?? string.Empty).Trim();
        model.Content = (model.Content ?? string.Empty).Trim();
        ValidateRequiredTrimmed(model.Title, nameof(model.Title), "Title is required.");
        ValidateRequiredTrimmed(model.Content, nameof(model.Content), "Content is required.");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        _context.ForumPosts.Add(new ForumPost
        {
            Title = model.Title,
            Content = model.Content,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Post created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var post = await GetPostWithCommentsAsync(id);
        if (post is null)
        {
            return NotFound();
        }

        return View(new ForumPostDetailsViewModel { Post = post });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var post = await _context.ForumPosts.FirstOrDefaultAsync(item => item.Id == id);
        if (post is null)
        {
            return NotFound();
        }

        if (post.UserId != userId)
        {
            return Forbid();
        }

        await ClearReportReferencesForPostAsync(post.Id);
        _context.ForumPosts.Remove(post);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Post deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int postId, CreateForumCommentViewModel model)
    {
        model.Content = (model.Content ?? string.Empty).Trim();
        ValidateRequiredTrimmed(model.Content, nameof(model.Content), "Comment is required.");

        var post = await GetPostWithCommentsAsync(postId);
        if (post is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Details", new ForumPostDetailsViewModel { Post = post, CommentForm = model });
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        _context.ForumComments.Add(new ForumComment
        {
            ForumPostId = postId,
            Content = model.Content,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var comment = await _context.ForumComments.FirstOrDefaultAsync(item => item.Id == id);
        if (comment is null)
        {
            return NotFound();
        }

        if (comment.UserId != userId)
        {
            return Forbid();
        }

        var postId = comment.ForumPostId;
        await ClearReportReferencesForCommentAsync(comment.Id);
        _context.ForumComments.Remove(comment);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Comment deleted.";
        return RedirectToAction(nameof(Details), new { id = postId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(CreateForumReportViewModel model)
    {
        model.Reason = (model.Reason ?? string.Empty).Trim();
        ValidateRequiredTrimmed(model.Reason, nameof(model.Reason), "Reason is required.");

        if (model.PostId is null && model.CommentId is null || model.PostId is not null && model.CommentId is not null)
        {
            ModelState.AddModelError(string.Empty, "Choose one forum item to report.");
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        ForumPost? post = null;
        ForumComment? comment = null;
        var redirectPostId = model.PostId;

        if (model.PostId is not null)
        {
            post = await _context.ForumPosts.FirstOrDefaultAsync(item => item.Id == model.PostId.Value);
            if (post is null)
            {
                return NotFound();
            }

            if (post.UserId == userId)
            {
                ModelState.AddModelError(string.Empty, "You cannot report your own post.");
            }
        }

        if (model.CommentId is not null)
        {
            comment = await _context.ForumComments.FirstOrDefaultAsync(item => item.Id == model.CommentId.Value);
            if (comment is null)
            {
                return NotFound();
            }

            redirectPostId = comment.ForumPostId;
            if (comment.UserId == userId)
            {
                ModelState.AddModelError(string.Empty, "You cannot report your own comment.");
            }
        }

        if (!ModelState.IsValid)
        {
            var detailsPost = await GetPostWithCommentsAsync(redirectPostId ?? 0);
            if (detailsPost is null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View("Details", new ForumPostDetailsViewModel { Post = detailsPost, ReportForm = model });
        }

        var duplicatePendingReport = await _context.ForumReports.AnyAsync(report =>
            report.ReporterUserId == userId &&
            report.Status == ForumReportStatuses.Pending &&
            report.PostId == model.PostId &&
            report.CommentId == model.CommentId);

        if (duplicatePendingReport)
        {
            TempData["ErrorMessage"] = "You already have a pending report for this item.";
            return RedirectToAction(nameof(Details), new { id = redirectPostId });
        }

        _context.ForumReports.Add(new ForumReport
        {
            ReporterUserId = userId,
            PostId = post?.Id,
            CommentId = comment?.Id,
            Reason = model.Reason,
            CreatedAt = DateTime.UtcNow,
            Status = ForumReportStatuses.Pending
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Report submitted.";
        return RedirectToAction(nameof(Details), new { id = redirectPostId });
    }

    private async Task<ForumPost?> GetPostWithCommentsAsync(int id)
    {
        return await _context.ForumPosts
            .Include(post => post.Comments.OrderBy(comment => comment.CreatedAt))
            .FirstOrDefaultAsync(post => post.Id == id);
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

    private void ValidateRequiredTrimmed(string value, string key, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError(key, message);
        }
    }
}
