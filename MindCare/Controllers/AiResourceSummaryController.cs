using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.Services.AI;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User)]
[Route("AI/Resources")]
public sealed class AiResourceSummaryController(
    ApplicationDbContext context,
    IAIService aiService,
    ILogger<AiResourceSummaryController> logger) : Controller
{
    [HttpPost("{resourceId:int}/Summarize")]
    [Produces("application/json")]
    [EnableRateLimiting("ai-resource-summary")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Summarize(int resourceId, CancellationToken cancellationToken)
    {
        var resource = await context.Resources
            .AsNoTracking()
            .Where(item => item.Id == resourceId && item.Status == ResourceStatus.Published)
            .Select(item => new { item.Title, item.Content })
            .SingleOrDefaultAsync(cancellationToken);

        if (resource is null)
        {
            return NotFound(new { success = false, message = "The requested resource was not found." });
        }

        if (string.IsNullOrWhiteSpace(resource.Content))
        {
            return BadRequest(new { success = false, message = "This resource does not contain enough text to summarize." });
        }

        try
        {
            var result = await aiService.SummarizeResourceAsync(resource.Title, resource.Content, cancellationToken);
            return Json(new { success = true, result.Summary, result.KeyPoints });
        }
        catch (ArgumentException)
        {
            return BadRequest(new { success = false, message = "This resource does not contain enough text to summarize." });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI resource summary request processing failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "The resource summary is temporarily unavailable. Please try again."
            });
        }
    }
}
