using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sachmoidaden.Models;
using sachmoidaden.Services;

namespace sachmoidaden.Controllers;
public abstract class ForumController(LibraryService library) : Controller
{
    protected LibraryService Library => library;
    protected int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    protected Actor Actor => new(UserId, User.FindFirstValue("stamp") ?? "");

    protected bool Valid(string? s, int max) => !string.IsNullOrWhiteSpace(s) && s.Length <= max;
    protected IActionResult Notice(string message, string url)
    {
        TempData["Notice"] = message;
        return LocalRedirect(url);
    }
}
