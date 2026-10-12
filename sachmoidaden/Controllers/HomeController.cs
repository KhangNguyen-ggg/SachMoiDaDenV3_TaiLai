using Microsoft.AspNetCore.Mvc;
using sachmoidaden.Models;
using sachmoidaden.Services;

namespace sachmoidaden.Controllers;
public class HomeController(LibraryService library) : ForumController(library)
{
    public async Task<IActionResult> Index()
    {
        var d = await Library.ReadAsync(UserId);
        var books = d.Stories.Where(b => b.IsPublic).ToList();
        var rankings = await Library.RankingsAsync(books);
        return View(new HomeViewModel(books.Take(3).ToList(), books.OrderByDescending(b => b.Views).Take(4).ToList(), books.OrderByDescending(b => b.CreatedAt).Take(4).ToList(), books.Where(b => b.Ratings.Count > 0).OrderByDescending(b => b.Ratings.Values.Average()).ThenByDescending(b => b.Ratings.Count).ThenBy(b => b.Title, StringComparer.Ordinal).Take(4).ToList(), d.Genres, rankings));
    }

    public async Task<IActionResult> Catalog(string? q, string? genre, string[]? genres, int page = 1)
    {
        var d = await Library.ReadAsync(UserId);
        q = (q ?? "").Trim();
        var selected = (genres ?? []).Append(genre ?? "").Where(g => g != "").Distinct().ToList();
        var key = ContentQueries.SearchKey(q);
        var books = d.Stories.Where(b => b.IsPublic && (selected.Count == 0 || b.Genres.Any(selected.Contains)) && ContentQueries.SearchKey(b.Title + " " + b.Author + " " + b.Genre).Contains(key)).ToList();
        int pages = Math.Max(1, (books.Count + 7) / 8);
        page = Math.Clamp(page, 1, pages);
        return View(new CatalogViewModel(books.Skip((page - 1) * 8).Take(8).ToList(), d.Genres, q, selected, page, pages, books.Count));
    }

    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
