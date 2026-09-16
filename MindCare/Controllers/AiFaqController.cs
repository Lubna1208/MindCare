using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MindCare.Services.AI;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize]
[Route("AI/Faq")]
public sealed class AiFaqController(IAIService aiService, ILogger<AiFaqController> logger) : Controller
{
    [HttpPost("Ask")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [EnableRateLimiting("ai-faq")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] FaqQuestionRequest? request, CancellationToken cancellationToken)
    {
        var question = request?.Question?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            return BadRequest(new { success = false, message = "Please enter a question." });
        }

        if (question.Length > 1000)
        {
            return BadRequest(new { success = false, message = "Questions can be up to 1,000 characters." });
        }

        try
        {
            var answer = await aiService.AskFaqAsync(question, cancellationToken);
            return Json(new { success = true, answer });
        }
        catch (ArgumentException)
        {
            return BadRequest(new { success = false, message = "Please enter a valid question." });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "AI FAQ request processing failed.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = "The assistant is temporarily unavailable. Please try again."
            });
        }
    }
}
