using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

[Authorize(Roles = RoleNames.User)]
public class AssessmentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AssessmentController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View(CreateForm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(AssessmentFormViewModel model)
    {
        ValidateSubmission(model);

        if (!ModelState.IsValid)
        {
            PopulateQuestionDetails(model);
            return View("Index", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null || string.IsNullOrWhiteSpace(user.Id))
        {
            return Challenge();
        }

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(applicationUser => applicationUser.Id == user.Id);
        if (!userExists)
        {
            return Challenge();
        }

        var answers = model.Questions
            .Select(answer =>
            {
                var question = AssessmentQuestionnaire.FindQuestion(answer.QuestionId)!;
                var option = question.Options.Single(option => option.Score == answer.SelectedScore);

                return new AssessmentAnswer
                {
                    QuestionId = question.Id,
                    SelectedOption = option.Text,
                    Score = option.Score
                };
            })
            .ToList();

        if (answers.Count != AssessmentQuestionnaire.Questions.Count ||
            answers.Any(answer => answer.QuestionId <= 0 ||
                                  string.IsNullOrWhiteSpace(answer.SelectedOption) ||
                                  answer.Score < 0 ||
                                  answer.Score > AssessmentQuestionnaire.MaximumOptionScore))
        {
            ModelState.AddModelError(string.Empty, "Your assessment answers could not be validated. Please review them and try again.");
            PopulateQuestionDetails(model);
            return View("Index", model);
        }

        var score = answers.Sum(answer => answer.Score);
        var outcome = AssessmentQuestionnaire.GetOutcome(score);
        var assessment = new Assessment
        {
            UserId = user.Id,
            User = user,
            Score = score,
            RiskLevel = outcome.RiskLevel,
            CreatedAt = DateTime.UtcNow,
            Answers = answers
        };

        foreach (var answer in answers)
        {
            // EF Core assigns AssessmentId after the new assessment is tracked and saved.
            answer.Assessment = assessment;
        }

        _context.Assessments.Add(assessment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Could not save assessment for user {UserId}. Database error: {DatabaseError}",
                user.Id,
                exception.InnerException?.Message);

            ModelState.AddModelError(string.Empty, "We could not save your assessment right now. Please try again later.");
            PopulateQuestionDetails(model);
            return View("Index", model);
        }

        return RedirectToAction(nameof(Result), new { id = assessment.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var assessment = await _context.Assessments
            .SingleOrDefaultAsync(assessment => assessment.Id == id && assessment.UserId == user.Id);
        if (assessment is null)
        {
            return NotFound();
        }

        return View(ToResultViewModel(assessment));
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var assessments = await _context.Assessments
            .Where(assessment => assessment.UserId == user.Id)
            .OrderByDescending(assessment => assessment.CreatedAt)
            .Select(assessment => new AssessmentResultViewModel
            {
                Id = assessment.Id,
                Score = assessment.Score,
                RiskLevel = assessment.RiskLevel,
                Recommendation = string.Empty,
                CreatedAt = assessment.CreatedAt
            })
            .ToListAsync();

        return View(assessments);
    }

    private static AssessmentFormViewModel CreateForm() => new()
    {
        Questions = AssessmentQuestionnaire.Questions
            .Select(question => new AssessmentQuestionFormViewModel
            {
                QuestionId = question.Id,
                QuestionText = question.Text,
                Options = question.Options
            })
            .ToList()
    };

    private static void PopulateQuestionDetails(AssessmentFormViewModel model)
    {
        var submittedAnswers = model.Questions
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.First());
        model.Questions = AssessmentQuestionnaire.Questions
            .Select(question =>
            {
                submittedAnswers.TryGetValue(question.Id, out var submittedAnswer);
                return new AssessmentQuestionFormViewModel
                {
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    Options = question.Options,
                    SelectedScore = submittedAnswer?.SelectedScore
                };
            })
            .ToList();
    }

    private void ValidateSubmission(AssessmentFormViewModel model)
    {
        if (model.Questions.Count != AssessmentQuestionnaire.Questions.Count ||
            model.Questions.Select(question => question.QuestionId).Distinct().Count() != model.Questions.Count)
        {
            ModelState.AddModelError(string.Empty, "Please answer every assessment question.");
            return;
        }

        foreach (var question in model.Questions)
        {
            var definition = AssessmentQuestionnaire.FindQuestion(question.QuestionId);
            if (definition is null || question.SelectedScore is null ||
                !definition.Options.Any(option => option.Score == question.SelectedScore))
            {
                ModelState.AddModelError(string.Empty, "Please provide a valid answer for every assessment question.");
                return;
            }
        }
    }

    private static AssessmentResultViewModel ToResultViewModel(Assessment assessment)
    {
        var outcome = AssessmentQuestionnaire.GetOutcome(assessment.Score);
        return new AssessmentResultViewModel
        {
            Id = assessment.Id,
            Score = assessment.Score,
            RiskLevel = assessment.RiskLevel,
            Recommendation = outcome.Recommendation,
            CreatedAt = assessment.CreatedAt
        };
    }
}
