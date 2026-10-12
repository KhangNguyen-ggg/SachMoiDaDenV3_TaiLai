using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sachmoidaden.Models;
using sachmoidaden.Services;

namespace sachmoidaden.Controllers;
public class StoryController(LibraryService library, ChapterFiles files) : ForumController(library)
{
    public async Task<IActionResult> Details(int id)
    {
        var d = await Library.ReadAsync(UserId);
        var b = d.Stories.FirstOrDefault(b => b.Id == id && b.IsPublic);
        if (b == null)
            return NotFound();
        ViewBag.Similar = ContentQueries.SimilarStories(d, b);
        return View(new StoryViewModel(b, ContentQueries.VisibleComments(d, id, null), d.Users.FirstOrDefault(u => u.Id == UserId)?.Saved.Contains(id) == true, b.Ratings.GetValueOrDefault(UserId)));
    }

    public async Task<IActionResult> File(int id)
    {
        var file = await Library.FileAsync(id, UserId);
        if (file == null)
            return NotFound();
        var stream = files.Open(file);
        if (stream == null)
            return NotFound("Tệp nội dung không còn trên server.");
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (file.ContentType == "application/pdf")
            Response.Headers["Content-Security-Policy"] = "sandbox; default-src 'none'";
        return File(stream, file.ContentType, enableRangeProcessing: true);
    }

    public async Task<IActionResult> Read(int id, int chapter = 1) => await OpenChapter(id, chapter, false);
    [Authorize(Roles = "Contributor,Moderator,Admin")]
    public async Task<IActionResult> Preview(int id, int chapter = 1) => await OpenChapter(id, chapter, true);
    async Task<IActionResult> OpenChapter(int id, int number, bool preview)
    {
        if (!ModelState.IsValid || number < 1)
            return BadRequest();
        var d = await Library.ReadAsync(UserId, preview);
        var b = d.Stories.FirstOrDefault(b => b.Id == id);
        if (b == null)
            return NotFound();
        var chapter = b.Chapters.FirstOrDefault(c => c.Number == number);
        if (chapter == null)
            return NotFound();
        var content = await files.PrepareAsync(chapter, HttpContext.RequestAborted);
        if (!preview)
        {
            Guid? guest = null;
            if (UserId == 0)
            {
                if (!Guid.TryParseExact(Request.Cookies["reader-id"], "N", out var guid))
                {
                    guid = Guid.NewGuid();
                    Response.Cookies.Append("reader-id", guid.ToString("N"), new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps, MaxAge = TimeSpan.FromDays(365) });
                }

                guest = guid;
            }

            await Library.RecordReadAsync(id, number, UserId, guest);
        }

        return View("Read", new ReaderViewModel(b, number, preview ? [] : ContentQueries.VisibleComments(d, null, null, chapter.Id), content, preview));
    }
}
