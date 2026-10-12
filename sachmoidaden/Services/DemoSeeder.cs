using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using sachmoidaden.Data;
using sachmoidaden.Models;

namespace sachmoidaden.Services;
public sealed class DemoBook
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public int Kind { get; set; }
    public bool Finished { get; set; }
    public bool Pending { get; set; }
    public bool Hidden { get; set; }
    public string[] Genres { get; set; } = [];
    public List<DemoChapter> Chapters { get; set; } = [];
}

public sealed class DemoChapter
{
    public string Title { get; set; } = "";
    public List<DemoFile> Files { get; set; } = [];
}

public sealed class DemoFile : ContentFile
{
    public string Sha256 { get; set; } = "";
}

// Explicit fixture data, not a fallback database. All relational rows commit together.
public sealed class DemoSeeder(SqlDatabase database, IWebHostEnvironment environment, IConfiguration configuration)
{
    public const string Password = "Demo123!";
    public static readonly (string Email, string Name, string Role, bool Locked)[] Accounts = [("admin@demo.local", "Admin mẫu", Roles.Admin, false), ("kiemduyet@demo.local", "Kiểm duyệt mẫu", Roles.Moderator, false), ("donggop@demo.local", "Người kể chuyện", Roles.Contributor, false), ("donggop2@demo.local", "Xưởng truyện tranh", Roles.Contributor, false), ("thanhvien@demo.local", "Độc giả An", Roles.Member, false), ("docgia2@demo.local", "Độc giả Bình", Roles.Member, false), ("docgia3@demo.local", "Độc giả Chi", Roles.Member, false), ("bikhoa@demo.local", "Tài khoản bị khóa", Roles.Member, true)];
    public async Task<bool> SeedAsync()
    {
        string bundle = Path.Combine(environment.ContentRootPath, "DemoData");
        var books = JsonSerializer.Deserialize<List<DemoBook>>(await File.ReadAllTextAsync(Path.Combine(bundle, "books.json"))) ?? throw new InvalidDataException("Thiếu danh sách truyện mẫu.");
        if (books.Count != 48 || books.Any(b => b.Chapters.Count != 6 || b.Chapters.Any(c => c.Files.Count == 0)))
            throw new InvalidDataException("Bộ truyện mẫu không đầy đủ.");
        var files = books.SelectMany(b => b.Chapters).SelectMany(c => c.Files).ToList();
        if (files.Select(f => f.Key).Distinct().Count() != files.Count || files.Any(f => !Guid.TryParseExact(f.Key, "N", out _)))
            throw new InvalidDataException("Khóa tệp mẫu không hợp lệ.");
        // A database extended property survives deletion of sample books/accounts. No extra table.
        const string markerQuery = "SELECT name FROM sys.extended_properties WHERE class=0 AND name=N'SachMoiDaDen.Demo.v1'";
        if (await database.ReadAsync(async s => (await s.QueryAsync(markerQuery)).Count > 0))
            return false;
        string target = Path.GetFullPath(configuration["Storage:Root"] ?? "App_Data/Chapters", environment.ContentRootPath);
        // ChapterFiles enforces the same outside-wwwroot storage rule.
        _ = new ChapterFiles(environment, configuration);
        foreach (var f in files)
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(bundle, "files", f.Key));
            if (bytes.LongLength != f.Size || !Convert.ToHexString(SHA256.HashData(bytes)).Equals(f.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Tệp mẫu bị thiếu hoặc thay đổi: {f.OriginalName}.");
            string path = Path.Combine(target, f.Key);
            try
            {
                await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await output.WriteAsync(bytes);
            }
            catch (IOException)when (File.Exists(path))
            {
                if (!SHA256.HashData(await File.ReadAllBytesAsync(path)).SequenceEqual(SHA256.HashData(bytes)))
                    throw new InvalidDataException($"Khóa tệp mẫu đã tồn tại với nội dung khác: {f.Key}.");
            }
        }

        // Keep copied files if commit outcome is uncertain. Re-running reuses identical bytes.
        return await database.TransactionAsync(async s =>
        {
            await s.ExecuteAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'SachMoiDaDen.Demo.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=15000; IF @r<0 THROW 50200,N'Khong lay duoc khoa tao du lieu mau.',1;");
            if ((await s.QueryAsync(markerQuery)).Count > 0)
                return false;
            foreach (var a in Accounts)
                if ((await s.QueryAsync("SELECT MaTaiKhoan FROM dbo.TaiKhoan WHERE Email=@email", ("@email", a.Email))).Count > 0)
                    throw new InvalidOperationException($"Email {a.Email} đã tồn tại nhưng bộ mẫu chưa được tạo. Không ghi đè hay nâng quyền tài khoản này; hãy dùng database thử nghiệm mới.");
            var now = (await s.QueryAsync("SELECT SYSUTCDATETIME() AS Now"))[0].Date("Now");
            var users = new List<int>();
            foreach (var a in Accounts)
            {
                var user = new AppUser
                {
                    Name = a.Name,
                    Email = a.Email,
                    Role = a.Role
                };
                users.Add(await Id(s, "INSERT dbo.TaiKhoan(TenHienThi,Email,MatKhauHash,MaVaiTro,BiKhoa) OUTPUT INSERTED.MaTaiKhoan AS Id VALUES(@name,@email,@hash,@role,@locked)", ("@name", a.Name), ("@email", a.Email), ("@hash", new PasswordHasher<AppUser>().HashPassword(user, Password)), ("@role", a.Role), ("@locked", a.Locked)));
            }

            var genres = (await s.QueryAsync("SELECT MaTheLoai,TenTheLoai FROM dbo.TheLoai")).ToDictionary(r => r.Text("TenTheLoai"), r => r.Int("MaTheLoai"));
            var bookIds = new List<int>();
            var chapters = new List<List<int>>();
            for (int i = 0; i < books.Count; i++)
            {
                var b = books[i];
                int id = await Id(s, "INSERT dbo.Sach(TenSach,TacGia,GioiThieu,LoaiTruyen,TrangThaiSangTac,MaNguoiDang,ChoDuyet,BiAn,NgayTao,NgayCapNhat) OUTPUT INSERTED.MaSach AS Id VALUES(@title,@author,@description,@kind,@finished,@owner,@pending,@hidden,@created,@updated)", ("@title", b.Title), ("@author", b.Author), ("@description", b.Description), ("@kind", b.Kind), ("@finished", b.Finished ? 1 : 0), ("@owner", users[2 + i % 2]), ("@pending", b.Pending), ("@hidden", b.Hidden), ("@created", now.AddDays(-60 - i)), ("@updated", now.AddHours(-i)));
                bookIds.Add(id);
                for (int g = 0; g < b.Genres.Length; g++)
                    await s.ExecuteAsync("INSERT dbo.SachTheLoai(MaSach,MaTheLoai,ThuTu) VALUES(@book,@genre,@order)", ("@book", id), ("@genre", genres[b.Genres[g]]), ("@order", g + 1));
                var ids = new List<int>();
                for (int c = 0; c < b.Chapters.Count; c++)
                {
                    int chapter = await Id(s, "INSERT dbo.Chuong(MaSach,SoChuong,TieuDe,NgayTao) OUTPUT INSERTED.MaChuong AS Id VALUES(@book,@number,@title,@created)", ("@book", id), ("@number", c + 1), ("@title", b.Chapters[c].Title), ("@created", now.AddDays(-30 + c)));
                    ids.Add(chapter);
                    foreach (var f in b.Chapters[c].Files)
                        await s.ExecuteAsync("INSERT dbo.NoiDungChuong(MaChuong,ThuTu,DuongDanTep,KieuTep,TenTepGoc,DungLuong) VALUES(@chapter,@order,@key,@mime,@name,@size)", ("@chapter", chapter), ("@order", f.Order), ("@key", f.Key), ("@mime", f.ContentType), ("@name", f.OriginalName), ("@size", f.Size));
                }

                chapters.Add(ids);
                if (b.Pending || b.Hidden)
                    continue;
                // Synthetic accepted-read events, spaced a day apart per account/book.
                for (int day = 0; day < 14; day++)
                    for (int reader = 0; reader < 1 + i % 3; reader++)
                        await s.ExecuteAsync("INSERT dbo.LuotDocSach(MaSach,MaTaiKhoan,ThoiDiem) VALUES(@book,@user,@time)", ("@book", id), ("@user", users[4 + reader]), ("@time", now.AddDays(-day).AddMinutes(-i - 1)));
                await s.ExecuteAsync("UPDATE dbo.Sach SET LuotDoc=(SELECT COUNT_BIG(*) FROM dbo.LuotDocSach WHERE MaSach=@book) WHERE MaSach=@book", ("@book", id));
            }

            var publicIndices = Enumerable.Range(0, books.Count).Where(i => !books[i].Pending && !books[i].Hidden).ToArray();
            for (int reader = 0; reader < 3; reader++)
            {
                for (int j = 0; j < 12; j++)
                {
                    int index = publicIndices[(j + reader * 8) % publicIndices.Length];
                    int book = bookIds[index], user = users[4 + reader];
                    await s.ExecuteAsync("INSERT dbo.DanhGia(MaTaiKhoan,MaSach,SoSao) VALUES(@user,@book,@stars)", ("@user", user), ("@book", book), ("@stars", 3 + (j + reader) % 3));
                    if (j < 8)
                        await s.ExecuteAsync("INSERT dbo.TuSach(MaTaiKhoan,MaSach) VALUES(@user,@book)", ("@user", user), ("@book", book));
                    if (j < 6)
                        await s.ExecuteAsync("INSERT dbo.LichSuDoc(MaTaiKhoan,MaSach,MaChuong,LanDocCuoi) VALUES(@user,@book,@chapter,@time)", ("@user", user), ("@book", book), ("@chapter", chapters[index][j % 6]), ("@time", now.AddHours(-j - reader)));
                }
            }

            var comments = new List<int>();
            for (int j = 0; j < publicIndices.Length; j++)
            {
                int index = publicIndices[j], book = bookIds[index];
                int root = await Comment(s, users[4], "Mình thích bối cảnh của truyện. Đây là bình luận mẫu để thử trả lời và báo cáo.", book: book);
                comments.Add(root);
                await Comment(s, users[5], "Mình cũng vậy! Bạn đã đọc đến chương 3 chưa?", book: book, parent: root);
                await Comment(s, users[6], "Chương mở đầu rõ ràng, mình sẽ đọc tiếp. [Dữ liệu mẫu]", chapter: chapters[index][0]);
                if (j < 6)
                {
                    int hidden = await Comment(s, users[5], "Bình luận mẫu đã bị ẩn bởi kiểm duyệt viên.", book: book, hidden: true);
                    await Comment(s, users[6], "Phản hồi này phải bị ẩn theo bình luận cha.", book: book, parent: hidden);
                }
            }

            string[] tagNames = ["goi-y", "ky-ao", "truyen-tranh", "thao-luan", "doc-sach", "viet-truyen"];
            var tags = new List<int>();
            foreach (string name in tagNames)
            {
                var existing = await s.QueryAsync("SELECT MaTag AS Id FROM dbo.Tag WHERE TenTag=@name", ("@name", name));
                tags.Add(existing.Count > 0 ? existing[0].Int("Id") : await Id(s, "INSERT dbo.Tag(TenTag) OUTPUT INSERTED.MaTag AS Id VALUES(@name)", ("@name", name)));
            }

            string[] topics = ["Bạn thích đọc truyện vào lúc nào?", "Gợi ý truyện kỳ ảo cho người mới", "Một khung tranh kể được bao nhiêu điều?", "Chia sẻ cách ghi chú khi đọc", "Thảo luận về kết thúc mở", "Luyện viết một đoạn mở đầu"];
            var posts = new List<int>();
            for (int i = 0; i < 18; i++)
            {
                int post = await Id(s, "INSERT dbo.BaiThaoLuan(MaTaiKhoan,TieuDe,NoiDung,BiAn,NgayTao) OUTPUT INSERTED.MaBaiThaoLuan AS Id VALUES(@user,@title,@body,@hidden,@created)", ("@user", users[2 + i % 5]), ("@title", $"{topics[i % 6]} · chủ đề {i + 1}"), ("@body", "[Bài thảo luận mẫu] Mình muốn nghe trải nghiệm của mọi người. Hãy chia sẻ một ví dụ, lý do bạn chọn cách đọc đó và một điều bạn muốn thử trong tuần tới. Bài này độc lập với sách, dùng để thử tag, gợi ý, bình luận và báo cáo."), ("@hidden", i == 17), ("@created", now.AddDays(-4).AddHours(-i)));
                posts.Add(post);
                for (int t = 0; t < 2; t++)
                    await s.ExecuteAsync("INSERT dbo.BaiThaoLuanTag(MaBaiThaoLuan,MaTag,ThuTu) VALUES(@post,@tag,@order)", ("@post", post), ("@tag", tags[(i + t) % tags.Count]), ("@order", t + 1));
                int root = await Comment(s, users[4], "Mình thường dành 20 phút đọc trước khi đi ngủ. [Bình luận mẫu]", post: post);
                await Comment(s, users[5], "Ý hay, mình sẽ thử và quay lại chia sẻ.", post: post, parent: root);
            }

            for (int i = 0; i < 12; i++)
            {
                int? book = i < 4 ? bookIds[publicIndices[i]] : null;
                int? post = i is >= 4 and < 8 ? posts[i - 4] : null;
                int? comment = i >= 8 ? comments[i - 8] : null;
                string snapshot = book != null ? books[publicIndices[i]].Title : post != null ? (await s.QueryAsync("SELECT CONCAT(TieuDe,NCHAR(10),NoiDung) AS Snapshot FROM dbo.BaiThaoLuan WHERE MaBaiThaoLuan=@post", ("@post", post)))[0].Text("Snapshot") : "Mình thích bối cảnh của truyện. Đây là bình luận mẫu để thử trả lời và báo cáo.";
                bool done = i % 2 == 0;
                await s.ExecuteAsync("INSERT dbo.BaoCao(MaNguoiBaoCao,MaSach,MaBaiThaoLuan,MaBinhLuan,NoiDungBiBaoCao,LyDo,NgayBaoCao,DaXuLy,MaNguoiXuLy,KetQuaXuLy,NgayXuLy) VALUES(@user,@book,@post,@comment,@snapshot,@reason,@created,@done,@resolver,@result,@resolved)", ("@user", users[4 + i % 3]), ("@book", book), ("@post", post), ("@comment", comment), ("@snapshot", snapshot), ("@reason", "[Báo cáo mẫu] Đề nghị kiểm tra nội dung để thử hàng đợi kiểm duyệt."), ("@created", now.AddDays(-2)), ("@done", done), ("@resolver", done ? users[1] : (int? )null), ("@result", done ? "[Kết quả mẫu] Đã kiểm tra, giữ nguyên nội dung." : null), ("@resolved", done ? now.AddDays(-1) : (DateTime? )null));
            }

            await s.ExecuteAsync("EXEC sys.sp_addextendedproperty @name=N'SachMoiDaDen.Demo.v1',@value=N'48 books / 288 chapters / 8 accounts';");
            return true;
        });
    }

    static async Task<int> Id(SqlSession session, string sql, params (string Name, object? Value)[] values) => (await session.QueryAsync(sql, values))[0].Int("Id");
    static Task<int> Comment(SqlSession s, int user, string body, int? book = null, int? chapter = null, int? post = null, int? parent = null, bool hidden = false) => Id(s, "DECLARE @ids TABLE(Id int); INSERT dbo.BinhLuan(MaTaiKhoan,NoiDung,MaSach,MaChuong,MaBaiThaoLuan,MaBinhLuanCha,BiAn,NgayTao) OUTPUT INSERTED.MaBinhLuan INTO @ids(Id) VALUES(@user,@body,@book,@chapter,@post,@parent,@hidden,DATEADD(day,-3,SYSUTCDATETIME())); SELECT Id FROM @ids;", ("@user", user), ("@body", body), ("@book", book), ("@chapter", chapter), ("@post", post), ("@parent", parent), ("@hidden", hidden));
}
