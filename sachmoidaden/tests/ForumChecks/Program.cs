using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using sachmoidaden.Data;
using sachmoidaden.Models;
using sachmoidaden.Services;

int checks = 0;
void Check(bool ok, string label)
{
    if (!ok)
        throw new Exception("FAIL " + label);
    checks++;
    Console.WriteLine("PASS " + label);
}

async Task Reject(Func<Task> action, string label)
{
    try
    {
        await action();
        throw new Exception("FAIL " + label);
    }
    catch (Exception e)when (e is BusinessException or InvalidDataException or Microsoft.Data.SqlClient.SqlException)
    {
        checks++;
        Console.WriteLine("PASS " + label);
    }
}

Check(ContentQueries.SearchKey("Đường RỪNG SƯƠNG") == "duong rung suong", "Vietnamese search without accents");
var d = new ForumData
{
    Stories = [new()
    {
        Id = 1,
        Genres = ["A", "B"],
        Chapters = [new()
        {
            Id = 10
        }

        ]
    }, new()
    {
        Id = 2,
        Genres = ["A"]
    }, new()
    {
        Id = 3,
        Genres = ["A", "B"],
        Hidden = true
    }

    ],
    Posts = [new()
    {
        Id = 20,
        Tags = ["Review"]
    }, new()
    {
        Id = 21,
        Tags = ["review"]
    }

    ]
};
Check(ContentQueries.SimilarStories(d, d.Stories[0]).Select(b => b.Id).SequenceEqual(new[] { 2 }), "recommendations exclude hidden books");
Check(ContentQueries.SimilarPosts(d, d.Posts[0]).Single().Id == 21, "forum recommendations use case-insensitive tags");
var root = new Comment
{
    Id = 30,
    StoryId = 1
};
var child = new Comment
{
    Id = 31,
    StoryId = 1,
    ParentId = 30
};
var grandchild = new Comment
{
    Id = 32,
    StoryId = 1,
    ParentId = 31
};
d.Comments = [root, child, grandchild];
Check(ContentQueries.IsPublic(d, grandchild), "visible reply chain");
root.Hidden = true;
Check(!ContentQueries.IsPublic(d, grandchild), "hidden ancestor hides descendants");
root.Hidden = false;
child.Hidden = true;
Check(!ContentQueries.IsPublic(d, grandchild), "restoring root preserves child's hidden state");
child.Hidden = false;
child.ChapterId = 10;
child.StoryId = null;
Check(!ContentQueries.IsPublic(d, child), "cross-scope reply rejected");
d.Stories[0].Hidden = true;
Check(ContentQueries.SimilarPosts(d, d.Posts[0]).Count == 1, "book visibility does not affect independent forum");
Check(ContentQueries.Vietnam(new DateTime(2026, 10, 4, 17, 0, 0, DateTimeKind.Utc)).Day == 5, "UTC+7 display crosses midnight");
var temp = Path.Combine(Path.GetTempPath(), "sach-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Storage:Root", "Chapters" } }).Build();
var files = new ChapterFiles(new TestEnvironment(temp), config);
IFormFile Upload(string name, byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "uploads", name);
try
{
    var saved = await files.SaveTextAsync("Tiếng Việt <script>alert(1)</script>", CancellationToken.None);
    var render = await files.PrepareAsync(new Chapter { Files = saved }, CancellationToken.None);
    Check(render.Single().Text!.Contains("Tiếng Việt"), "text stored as persistent UTF-8 TXT");
    using (var reopened = new ChapterFiles(new TestEnvironment(temp), config).Open(saved[0]))
        Check(reopened != null, "file survives service restart");
    var md = await files.SaveAsync([Upload("test.md", Encoding.UTF8.GetBytes("# Xin chào"))], null, CancellationToken.None);
    Check(md[0].ContentType == "text/markdown", "Markdown metadata");
    var pdf = await files.SaveAsync([Upload("test.pdf", Encoding.ASCII.GetBytes("%PDF-1.4\nfixture\n%%EOF"))], null, CancellationToken.None);
    Check(pdf[0].ContentType == "application/pdf", "PDF signature accepted");
    var fixtures = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("tests/image-fixtures.json"))!;
    var pictures = await files.SaveAsync([Upload("a.png", Convert.FromBase64String(fixtures["png"])), Upload("b.jpeg", Convert.FromBase64String(fixtures["jpeg"])), Upload("c.webp", Convert.FromBase64String(fixtures["webp"]))], [2, 0, 1], CancellationToken.None);
    Check(pictures[0].ContentType == "image/webp", "valid WebP accepted");
    Check(pictures[1].ContentType == "image/png", "valid PNG accepted");
    Check(pictures[2].ContentType == "image/jpeg", "valid JPEG accepted");
    Check(pictures.Select(f => f.OriginalName).SequenceEqual(new[] { "c.webp", "a.png", "b.jpeg" }), "image order preserved");
    files.Delete(pictures);
    await Reject(() => files.SaveAsync([Upload("fake.png", Encoding.UTF8.GetBytes("hello"))], null, CancellationToken.None), "fake image rejected");
    await Reject(() => files.SaveAsync([Upload("fake.pdf", Encoding.UTF8.GetBytes("hello"))], null, CancellationToken.None), "fake PDF rejected");
    await Reject(() => files.SaveAsync([Upload("bad.txt", [255, 254, 0, 0])], null, CancellationToken.None), "invalid UTF-8 rejected");
    await Reject(() => files.SaveAsync([Upload("bad.docx", [1, 2, 3])], null, CancellationToken.None), "unsupported file rejected");
    await Reject(() => files.SaveAsync([Upload("empty.txt", [])], null, CancellationToken.None), "empty file rejected");
    await Reject(() => files.SaveAsync([Upload("test.txt", Encoding.UTF8.GetBytes("valid"))], [1], CancellationToken.None), "invalid file order rejected");
    int before = Directory.GetFiles(Path.Combine(temp, "Chapters")).Length;
    await Reject(() => files.SaveAsync([Upload("ok.txt", Encoding.UTF8.GetBytes("valid")), Upload("bad.png", [1, 2, 3])], null, CancellationToken.None), "partial batch rejected");
    Check(Directory.GetFiles(Path.Combine(temp, "Chapters")).Length == before, "partial upload cleaned");
    files.Delete(saved);
    await Reject(() => files.PrepareAsync(new Chapter { Files = saved }, CancellationToken.None), "missing content rejected before reading");
    files.Delete(md);
    files.Delete(pdf);
    using var demo = System.Text.Json.JsonDocument.Parse(File.ReadAllText("DemoData/books.json"));
    var demoBooks = demo.RootElement.EnumerateArray().ToArray();
    Check(demoBooks.Length == 48 && demoBooks.Sum(b => b.GetProperty("Chapters").GetArrayLength()) == 288, "demo has 48 books and 288 chapters");
    Check(demoBooks.Count(b => !b.GetProperty("Pending").GetBoolean() && !b.GetProperty("Hidden").GetBoolean()) == 36 && demoBooks.Count(b => b.GetProperty("Pending").GetBoolean() && !b.GetProperty("Hidden").GetBoolean()) == 8 && demoBooks.Count(b => b.GetProperty("Hidden").GetBoolean()) == 4, "demo covers public, pending and hidden books");
    Check(demoBooks.Where(b => !b.GetProperty("Pending").GetBoolean() && !b.GetProperty("Hidden").GetBoolean()).Any(b => b.GetProperty("Kind").GetInt32() == 1), "public demo includes comics");
    var allKeys = new HashSet<string>();
    var allTypes = new HashSet<string>();
    int demoFileCount = 0;
    foreach (var book in demoBooks)
        foreach (var chapter in book.GetProperty("Chapters").EnumerateArray())
        {
            var metadata = chapter.GetProperty("Files").EnumerateArray().ToArray();
            var uploads = new List<IFormFile>();
            foreach (var f in metadata)
            {
                string key = f.GetProperty("Key").GetString()!;
                if (!Guid.TryParseExact(key, "N", out _) || !allKeys.Add(key))
                    throw new Exception("Invalid/duplicate demo key");
                var bytes = File.ReadAllBytes(Path.Combine("DemoData", "files", key));
                if (bytes.LongLength != f.GetProperty("Size").GetInt64() || !Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).Equals(f.GetProperty("Sha256").GetString(), StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Demo checksum/size mismatch: " + key);
                uploads.Add(Upload(f.GetProperty("OriginalName").GetString()!, bytes));
                allTypes.Add(f.GetProperty("ContentType").GetString()!);
            }

            var imported = await files.SaveAsync(uploads, null, CancellationToken.None);
            for (int i = 0; i < imported.Count; i++)
                if (imported[i].Order != metadata[i].GetProperty("Order").GetInt32() || imported[i].ContentType != metadata[i].GetProperty("ContentType").GetString())
                    throw new Exception("Demo MIME/order mismatch");
            var prepared = await new ChapterFiles(new TestEnvironment(temp), config).PrepareAsync(new Chapter { Files = imported }, CancellationToken.None);
            if (prepared.Count != metadata.Length)
                throw new Exception("Demo chapter not fully readable");
            demoFileCount += imported.Count;
            files.Delete(imported);
        }

    Check(allKeys.Count == 432 && demoFileCount == 432, "all 432 demo files pass checksums, upload validation, order and persistent reader");
    Check(allTypes.SetEquals(new[] { "text/plain", "text/markdown", "application/pdf", "image/png" }), "demo covers TXT, Markdown, PDF and PNG readers");
}
finally
{
    Directory.Delete(temp, true);
}

Console.WriteLine($"{checks} CORE CHECKS PASSED");
var testConnection = Environment.GetEnvironmentVariable("SACH_TEST_CONNECTION");
if (string.IsNullOrEmpty(testConnection))
{
    Console.WriteLine("SKIP SQL checks: set SACH_TEST_CONNECTION to a V3 schema database ending in _Tests.");
    return;
}

var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(testConnection);
if (!connection.InitialCatalog.EndsWith("_Tests", StringComparison.Ordinal))
    throw new Exception("Use an isolated database ending in _Tests.");
var db = new SqlDatabase(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "ConnectionStrings:SachMoiDaDen", testConnection } }).Build());
var service = new LibraryService(db);
var seedConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Storage:Root", "App_Data/TestDemoChapters" } }).Build();
var seeder = new DemoSeeder(db, new TestEnvironment(Directory.GetCurrentDirectory()), seedConfig);
await seeder.SeedAsync();
var seedBefore = await db.ReadAsync(s => s.QueryAsync("SELECT Email,MatKhauHash,MaVaiTro,BiKhoa,DauBaoMat FROM dbo.TaiKhoan WHERE Email LIKE '%@demo.local' ORDER BY Email"));
Check(!await seeder.SeedAsync(), "demo repeat skips committed seed");
var seedAfter = await db.ReadAsync(s => s.QueryAsync("SELECT Email,MatKhauHash,MaVaiTro,BiKhoa,DauBaoMat FROM dbo.TaiKhoan WHERE Email LIKE '%@demo.local' ORDER BY Email"));
Check(seedBefore.Count == 8 && System.Text.Json.JsonSerializer.Serialize(seedBefore) == System.Text.Json.JsonSerializer.Serialize(seedAfter), "demo repeat preserves account hashes, roles, locks and stamps");
Check(await service.ValidateAsync("admin@demo.local", DemoSeeder.Password)is { Role: Roles.Admin }, "demo admin can log in");
Check(await service.ValidateAsync("bikhoa@demo.local", DemoSeeder.Password) == null, "locked demo account cannot log in");
string token = Guid.NewGuid().ToString("N"), password = "Test12345!";
var actors = new Dictionary<string, Actor>();
foreach (var role in Roles.All)
{
    string email = $"checks-{role}-{token}@example.test";
    Check(await service.RegisterAsync("Check " + role, email, password), "register " + role);
    Check(!await service.RegisterAsync("Duplicate", email.ToUpperInvariant(), password), "duplicate email " + role);
    var user = await service.ValidateAsync(email, password) ?? throw new Exception("login failed");
    await db.ReadAsync(s => s.ExecuteAsync("UPDATE dbo.TaiKhoan SET MaVaiTro=@role WHERE MaTaiKhoan=@id", ("@role", role), ("@id", user.Id)));
    actors[role] = new Actor(user.Id, user.Stamp);
}

var admin = actors[Roles.Admin];
var writer = actors[Roles.Contributor];
var member = actors[Roles.Member];
var moderator = actors[Roles.Moderator];
var genres = await service.GenresAsync();
EditBookInput Book() => new()
{
    Title = "Kiểm tra " + token,
    Author = "Nhóm 2",
    Description = "Fixture kiểm tra V3",
    Genres = genres.Take(2).ToList()
};
await Reject(() => service.SaveBookAsync(member, Book()), "Member cannot publish");
await Reject(() => service.SaveBookAsync(moderator, Book()), "Moderator cannot edit books");
int id = await service.SaveBookAsync(writer, Book());
Check(!(await service.ReadAsync()).Stories.Any(b => b.Id == id), "contributor book pending");
await Reject(() => service.EnsureBookOwnerAsync(new Actor(member.Id, member.Stamp), id), "non-owner cannot add chapter");
var storage = Path.Combine(Path.GetTempPath(), "sach-sql-tests-" + token);
Directory.CreateDirectory(storage);
var disk = new ChapterFiles(new TestEnvironment(storage), config);
var chapterFiles = await disk.SaveTextAsync("Nội dung kiểm tra", CancellationToken.None);
int chapterId = await service.AddChapterAsync(writer, id, "Chương một", chapterFiles);
await service.ModerateAsync(moderator, "book", id, "hide");
await service.ModerateAsync(moderator, "book", id, "approve");
Check(!(await service.ReadAsync()).Stories.Any(b => b.Id == id), "approval preserves hidden flag");
await service.ModerateAsync(moderator, "book", id, "restore");
var data = await service.ReadAsync(member.Id);
Check(data.Stories.Any(b => b.Id == id), "restored approved book public");
await disk.PrepareAsync(data.Stories.First(b => b.Id == id).Chapters.Single(), CancellationToken.None);
await service.RecordReadAsync(id, 1, member.Id, null);
await service.RecordReadAsync(id, 1, member.Id, null);
var counted = await db.ReadAsync(s => s.QueryAsync("SELECT COUNT(*) AS N FROM dbo.LuotDocSach WHERE MaSach=@id AND MaTaiKhoan=@u", ("@id", id), ("@u", member.Id)));
Check(counted[0].Int("N") == 1, "30 minute read dedup");
Check((await service.ReadAsync(member.Id)).Users.Single().History[id] == chapterId, "history stores chapter ID");
await service.SaveAsync(member, id, true);
await service.SaveAsync(member, id, true);
Check((await service.ReadAsync(member.Id)).Users.Single().Saved.Contains(id), "idempotent bookshelf save");
await service.SaveAsync(member, id, false);
await service.SaveAsync(member, id, false);
Check(!(await service.ReadAsync(member.Id)).Users.Single().Saved.Contains(id), "idempotent bookshelf remove");
await service.RateAsync(member, id, 4);
await service.RateAsync(member, id, 5);
Check((await service.ReadAsync()).Stories.First(b => b.Id == id).Ratings[member.Id] == 5, "rating updated in place");
int postId = await service.CreateDiscussionAsync(member, new() { Title = "Test forum", Body = "Independent discussion", Tags = "review,Review,test" });
int commentId = await service.CommentAsync(member, null, chapterId, null, null, "Bình luận chương");
int replyId = await service.CommentAsync(member, null, chapterId, null, commentId, "Phản hồi");
await Reject(() => service.CommentAsync(member, null, null, postId, commentId, "cross scope"), "cross-scope reply blocked by SQL");
await service.ReportAsync(member, "comment", replyId, "Cần xem lại");
var report = (await service.ReadAsync(admin.Id, true)).Reports.Single(r => r.TargetId == replyId);
var results = await Task.WhenAll(service.ResolveAsync(admin, report.Id, "Đã kiểm tra"), service.ResolveAsync(moderator, report.Id, "Kết quả khác"));
Check(results.Count(x => x) == 1, "concurrent resolve does not overwrite");
await service.ModerateAsync(moderator, "comment", commentId, "hide");
Check(!(await service.ReadAsync()).Comments.Any(c => c.Id == replyId), "hidden parent removes reply from public view");
await service.ModerateAsync(moderator, "book", id, "hide");
var hidden = (await service.ReadAsync(admin.Id, true)).Stories.First(b => b.Id == id);
var fileId = hidden.Chapters[0].Files[0].Id;
Check(await service.FileAsync(fileId, member.Id) == null, "hidden file inaccessible to reader");
Check(await service.FileAsync(fileId, moderator.Id) != null, "hidden file accessible to moderator");
Check((await service.ReadAsync()).Posts.Any(p => p.Id == postId), "forum remains public when book hidden");
await Reject(() => service.GenreAsync(admin, "delete", genres[0]), "used genre cannot be deleted");
await Reject(() => service.LockUserAsync(admin, admin.Id, true), "admin cannot lock self");
await Reject(() => service.UserRoleAsync(admin, admin.Id, Roles.Member), "admin cannot demote self");
await service.UserRoleAsync(admin, writer.Id, Roles.Member);
Check((await service.FindUserAsync(writer.Id))!.Stamp != writer.Stamp, "role change rotates security stamp");
await Reject(() => service.EnsureBookOwnerAsync(writer, id), "stale contributor session blocked");
await service.LockUserAsync(admin, member.Id, true);
Check(await service.ValidateAsync($"checks-{Roles.Member}-{token}@example.test", password) == null, "locked account cannot log in");
Console.WriteLine($"{checks} CHECKS PASSED; SQL fixtures kept in isolated test database, marker {token}. Storage: {storage}");
sealed class TestEnvironment(string root) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "sachmoidaden";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = root;
    public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
