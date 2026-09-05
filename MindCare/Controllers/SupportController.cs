using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MindCare.Data;
using MindCare.Models;
using MindCare.ViewModels;

namespace MindCare.Controllers;

public class SupportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SupportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Immediate() => View();

    [Authorize(Roles = RoleNames.User)]
    [HttpGet]
    public async Task<IActionResult> TrustedPeople()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        var contacts = await _context.TrustedContacts
            .AsNoTracking()
            .Where(contact => contact.UserId == user.Id)
            .OrderBy(contact => contact.Name)
            .ToListAsync();

        return View(contacts);
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpGet]
    public IActionResult AddTrustedPerson() => View(new TrustedContactViewModel());

    [Authorize(Roles = RoleNames.User)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrustedPerson(TrustedContactViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.TrustedContacts.Add(new TrustedContact
        {
            UserId = user.Id,
            Name = model.Name.Trim(),
            Relationship = model.Relationship.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trusted person saved.";
        return RedirectToAction(nameof(TrustedPeople));
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpGet]
    public async Task<IActionResult> EditTrustedPerson(int id)
    {
        var contact = await GetOwnedContactAsync(id);
        if (contact is null)
        {
            return NotFound();
        }

        return View(new TrustedContactViewModel
        {
            Name = contact.Name,
            Relationship = contact.Relationship,
            PhoneNumber = contact.PhoneNumber
        });
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTrustedPerson(int id, TrustedContactViewModel model)
    {
        var contact = await GetOwnedContactAsync(id);
        if (contact is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        contact.Name = model.Name.Trim();
        contact.Relationship = model.Relationship.Trim();
        contact.PhoneNumber = model.PhoneNumber.Trim();
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trusted person updated.";
        return RedirectToAction(nameof(TrustedPeople));
    }

    [Authorize(Roles = RoleNames.User)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTrustedPerson(int id)
    {
        var contact = await GetOwnedContactAsync(id);
        if (contact is null)
        {
            return NotFound();
        }

        _context.TrustedContacts.Remove(contact);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trusted person removed.";
        return RedirectToAction(nameof(TrustedPeople));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync() => await _userManager.GetUserAsync(User);

    private async Task<TrustedContact?> GetOwnedContactAsync(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return null;
        }

        return await _context.TrustedContacts
            .SingleOrDefaultAsync(contact => contact.Id == id && contact.UserId == user.Id);
    }
}
