using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sachmoidaden.Models;
using sachmoidaden.Services;

namespace sachmoidaden.Controllers;
public class DiscussionController(LibraryService library) : ForumController(library)
{
    public async Task<IActionResult> Index(string? tag, int page = 1)
    {
        var d = await Library.ReadAsync();
        var all = d.Posts;
        var posts = all.Where(p => string.IsNullOrEmpty(tag) || p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        int pages = Math.Max(1, (posts.Count + 9) / 10);
        page = Math.Clamp(page, 1, pages);
        return View(new ForumViewModel(posts.Skip((page - 1) * 10).Take(10).ToList(), all.SelectMany(p => p.Tags).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList(), tag, page, pages));
    }

    public async Task<IActionResult> Details(int id)
    {
        var d = await Library.ReadAsync();
        var p = d.Posts.FirstOrDefault(p => p.Id == id && !p.Hidden);
        if (p == null)
            return NotFound();
        ViewBag.Similar = ContentQueries.SimilarPosts(d, p);
        return View(new DiscussionViewModel(p, ContentQueries.VisibleComments(d, null, id)));
    }

    [Authorize, HttpGet]
    public IActionResult Create() => View(new CreateDiscussionInput());
    [Authorize, HttpPost]
    public async Task<IActionResult> Create(CreateDiscussionInput input)
    {
        if (!ModelState.IsValid)
            return View(input);
        try
        {
            int id = await Library.CreateDiscussionAsync(Actor, input);
            return RedirectToAction("Details", new { id });
        }
        catch (BusinessException e)when (e.Status == 400)
        {
            ModelState.AddModelError("", e.Message);
            return View(input);
        }
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Comment(int? storyId, int? chapterId, int? postId, int? parentId, string text)
    {
        if (!ModelState.IsValid || !Valid(text, 2000))
            return BadRequest("Nhập bình luận từ 1 đến 2000 ký tự.");
        int id = await Library.CommentAsync(Actor, storyId, chapterId, postId, parentId, text);
        string url = postId.HasValue ? $"/Discussion/Details/{postId}" : $"/Story/Details/{storyId}";
        if (chapterId.HasValue)
        {
            var d = await Library.ReadAsync();
            var b = d.Stories.First(b => b.Chapters.Any(c => c.Id == chapterId));
            url = $"/Story/Read/{b.Id}?chapter={b.Chapters.First(c => c.Id == chapterId).Number}";
        }

        return Notice("Đã đăng bình luận.", url + $"#comment-{id}");
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Rate(int storyId, int value)
    {
        if (!ModelState.IsValid || value is < 1 or > 5)
            return BadRequest();
        await Library.RateAsync(Actor, storyId, value);
        return Notice("Đã lưu đánh giá của bạn.", $"/Story/Details/{storyId}");
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Report(string kind, int id, string reason)
    {
        await Library.ReportAsync(Actor, kind, id, reason);
        return Notice("Đã gửi báo cáo đến người kiểm duyệt.", "/Home/Catalog");
    }
}
