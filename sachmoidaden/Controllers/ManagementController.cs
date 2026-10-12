using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sachmoidaden.Models;
using sachmoidaden.Services;

namespace sachmoidaden.Controllers;
[Authorize(Roles = "Contributor,Moderator,Admin")]
public class ManagementController(LibraryService library, ChapterFiles files, ILogger<ManagementController> logger) : ForumController(library)
{
    bool Admin => User.IsInRole(Roles.Admin);

    bool Reviewer => Admin || User.IsInRole(Roles.Moderator);

    public async Task<IActionResult> Index(string tab = "books")
    {
        if (!new[]
        {
            "books",
            "reports",
            "posts",
            "genres",
            "users"
        }.Contains(tab))
            return BadRequest();
        if ((tab is "genres" or "users") && !Admin || (tab is "reports" or "posts") && !Reviewer)
            return Forbid();
        ViewBag.Tab = tab;
        return View(await Library.ReadAsync(UserId, true));
    }

    public async Task<IActionResult> Preview(int id)
    {
        var d = await Library.ReadAsync(UserId, true);
        var b = d.Stories.FirstOrDefault(b => b.Id == id);
        return b == null ? NotFound() : View(b);
    }

    [Authorize(Roles = "Contributor,Admin"), HttpGet]
    public async Task<IActionResult> Book(int id = 0)
    {
        ViewBag.Genres = await Library.GenresAsync();
        if (id == 0)
            return View(new EditBookInput());
        await Library.EnsureBookOwnerAsync(Actor, id);
        var b = (await Library.ReadAsync(UserId, true)).Stories.FirstOrDefault(b => b.Id == id);
        if (b == null)
            return NotFound();
        return View(new EditBookInput { Id = id, Title = b.Title, Author = b.Author, Genres = b.Genres, Kind = b.Kind, Status = b.Status, Description = b.Description });
    }

    [Authorize(Roles = "Contributor,Admin"), HttpPost]
    public async Task<IActionResult> Book(EditBookInput input)
    {
        ViewBag.Genres = await Library.GenresAsync();
        if (!ModelState.IsValid)
            return View(input);
        try
        {
            int id = await Library.SaveBookAsync(Actor, input);
            return Notice(Admin ? "Đã lưu thông tin sách." : "Đã gửi sách chờ duyệt.", $"/Management/Book/{id}");
        }
        catch (BusinessException e)when (e.Status == 400)
        {
            ModelState.AddModelError("", e.Message);
            return View(input);
        }
    }

    [Authorize(Roles = "Contributor,Admin"), HttpPost]
    public async Task<IActionResult> Chapter(int id, string title, string content)
    {
        if (!ModelState.IsValid || !Valid(title, 160) || !Valid(content, 100000))
            return BadRequest("Nhập tên chương 1–160 và nội dung 1–100.000 ký tự.");
        await Library.EnsureBookOwnerAsync(Actor, id);
        var saved = await files.SaveTextAsync(content, HttpContext.RequestAborted);
        return await Attach(id, title, saved);
    }

    [Authorize(Roles = "Contributor,Admin"), HttpPost, RequestSizeLimit(ChapterFiles.MaxRequestBytes)]
    public async Task<IActionResult> ComicChapter(int id, string title, List<IFormFile> pages, string order)
    {
        if (!ModelState.IsValid || !Valid(title, 160))
            return BadRequest("Tên chương bắt buộc, tối đa 160 ký tự.");
        await Library.EnsureBookOwnerAsync(Actor, id);
        var b = (await Library.ReadAsync(UserId, true)).Stories.First(s => s.Id == id);
        if (b.Kind != StoryKind.Comic)
            return BadRequest("Dùng form tệp cho chương của truyện chữ.");
        if (pages.Any(p => Path.GetExtension(p.FileName).ToLowerInvariant()is not (".png" or ".jpg" or ".jpeg" or ".webp")))
            return BadRequest("Form ảnh chỉ nhận JPG, PNG hoặc WebP.");
        List<int>? indices;
        try
        {
            indices = System.Text.Json.JsonSerializer.Deserialize<List<int>>(order);
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest("Thứ tự ảnh không hợp lệ.");
        }

        if (indices == null)
            return BadRequest();
        return await Attach(id, title, await files.SaveAsync(pages, indices, HttpContext.RequestAborted));
    }

    [Authorize(Roles = "Contributor,Admin"), HttpPost, RequestSizeLimit(ChapterFiles.MaxRequestBytes)]
    public async Task<IActionResult> FileChapter(int id, string title, List<IFormFile> uploads, string? order)
    {
        if (!ModelState.IsValid || !Valid(title, 160))
            return BadRequest("Tên chương bắt buộc, tối đa 160 ký tự.");
        await Library.EnsureBookOwnerAsync(Actor, id);
        List<int>? indices = null;
        if (order != null)
        {
            try
            {
                indices = System.Text.Json.JsonSerializer.Deserialize<List<int>>(order);
            }
            catch (System.Text.Json.JsonException)
            {
                return BadRequest("Thứ tự tệp không hợp lệ.");
            }

            if (indices == null)
                return BadRequest();
        }

        return await Attach(id, title, await files.SaveAsync(uploads, indices, HttpContext.RequestAborted));
    }

    async Task<IActionResult> Attach(int id, string title, List<ContentFile> saved)
    {
        try
        {
            await Library.AddChapterAsync(Actor, id, title, saved);
        }
        catch (Exception failure)
        {
            bool committed;
            try
            {
                committed = await Library.FilesCommittedAsync(saved);
            }
            catch (Exception confirmation)
            {
                logger.LogError(confirmation, "Cannot confirm chapter commit; preserving files {Keys}", string.Join(",", saved.Select(f => f.Key)));
                throw new BusinessException("Chưa xác nhận được việc lưu chương. Tệp được giữ để tránh mất nội dung; kiểm tra danh sách chương trước khi gửi lại.", 503);
            }

            if (!committed)
            {
                files.Delete(saved);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        return Notice("Đã thêm chương." + (Admin ? "" : " Sách đang chờ duyệt lại."), $"/Management/Book/{id}");
    }

    [Authorize(Roles = "Moderator,Admin"), HttpPost]
    public async Task<IActionResult> Moderate(string kind, int id, string action)
    {
        await Library.ModerateAsync(Actor, kind, id, action);
        return Notice("Đã cập nhật trạng thái nội dung.", "/Management?tab=" + (kind == "book" ? "books" : "posts"));
    }

    [Authorize(Roles = "Moderator,Admin"), HttpPost]
    public async Task<IActionResult> Resolve(int id, string resolution)
    {
        bool ok = await Library.ResolveAsync(Actor, id, resolution);
        return Notice(ok ? "Đã ghi nhận kết quả xử lý." : "Báo cáo đã được xử lý hoặc không còn tồn tại. Kết quả trước được giữ nguyên.", "/Management?tab=reports");
    }

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<IActionResult> Genre(string name)
    {
        await Library.GenreAsync(Actor, "add", name);
        return Notice("Đã thêm thể loại.", "/Management?tab=genres");
    }

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<IActionResult> RenameGenre(string name, string newName)
    {
        await Library.GenreAsync(Actor, "rename", name, newName);
        return Notice("Đã đổi tên thể loại.", "/Management?tab=genres");
    }

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<IActionResult> DeleteGenre(string name)
    {
        await Library.GenreAsync(Actor, "delete", name);
        return Notice("Đã xóa thể loại chưa được sử dụng.", "/Management?tab=genres");
    }

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<IActionResult> UserRole(int id, string role)
    {
        await Library.UserRoleAsync(Actor, id, role);
        return Notice("Đã cập nhật vai trò và kết thúc phiên cũ.", "/Management?tab=users");
    }

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<IActionResult> ToggleUser(int id, bool? locked)
    {
        if (!ModelState.IsValid || locked == null)
            return BadRequest();
        await Library.LockUserAsync(Actor, id, locked.Value);
        return Notice("Đã cập nhật tài khoản.", "/Management?tab=users");
    }
}
