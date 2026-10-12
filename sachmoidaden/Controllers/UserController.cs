using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using sachmoidaden.Services;

namespace sachmoidaden.Controllers;
[Authorize]
public class UserController(LibraryService library) : ForumController(library)
{
    public async Task<IActionResult> Following()
    {
        var d = await Library.ReadAsync(UserId);
        var u = d.Users.First(u => u.Id == UserId);
        return View(d.Stories.Where(b => u.Saved.Contains(b.Id)).ToList());
    }

    public async Task<IActionResult> History()
    {
        var d = await Library.ReadAsync(UserId);
        var history = d.Users.First(u => u.Id == UserId).History;
        ViewBag.History = history;
        return View(history.Keys.Select(id => d.Stories.FirstOrDefault(b => b.Id == id)).OfType<sachmoidaden.Models.Story>().ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Save(int id, bool? saved)
    {
        if (!ModelState.IsValid || saved == null)
            return BadRequest("Chỉ định trạng thái lưu sách.");
        await Library.SaveAsync(Actor, id, saved.Value);
        return Notice(saved.Value ? "Đã thêm vào tủ sách." : "Đã bỏ khỏi tủ sách.", $"/Story/Details/{id}");
    }
}
