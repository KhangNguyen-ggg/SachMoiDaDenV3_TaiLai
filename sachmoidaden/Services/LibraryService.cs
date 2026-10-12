using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using sachmoidaden.Data;
using sachmoidaden.Models;

namespace sachmoidaden.Services;
public sealed class BusinessException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}

public sealed class LibraryService(SqlDatabase db)
{
    readonly PasswordHasher<AppUser> hasher = new();
    public static ClaimsPrincipal Principal(AppUser u) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Name), new Claim(ClaimTypes.Role, u.Role), new Claim("stamp", u.Stamp) }, "Cookies"));
    static AppUser User(SqlRow r) => new()
    {
        Id = r.Int("MaTaiKhoan"),
        Name = r.Text("TenHienThi"),
        Email = r.Text("Email"),
        Role = r.Text("MaVaiTro"),
        Locked = r.Bool("BiKhoa"),
        Stamp = r.Text("DauBaoMat"),
        PasswordHash = r.ContainsKey("MatKhauHash") ? r.Text("MatKhauHash") : ""
    };
    static ContentFile FileRow(SqlRow r) => new()
    {
        Id = r.Int("MaNoiDung"),
        Order = r.ContainsKey("ThuTu") ? r.Int("ThuTu") : 0,
        Key = r.Text("DuongDanTep"),
        ContentType = r.Text("KieuTep"),
        OriginalName = r.Text("TenTepGoc"),
        Size = r.Long("DungLuong")
    };
    public Task<AppUser?> FindUserAsync(int id) => db.ReadAsync(async s =>
    {
        var rows = await s.QueryAsync("SELECT MaTaiKhoan,TenHienThi,Email,MaVaiTro,BiKhoa,DauBaoMat FROM dbo.TaiKhoan WHERE MaTaiKhoan=@id", ("@id", id));
        return rows.Count == 0 ? null : User(rows[0]);
    });
    public Task<AppUser?> ValidateAsync(string email, string password) => db.ReadAsync(async s =>
    {
        var rows = await s.QueryAsync("SELECT * FROM dbo.TaiKhoan WHERE Email=@email", ("@email", email.Trim()));
        if (rows.Count == 0)
            return null;
        var u = User(rows[0]);
        if (u.Locked)
            return null;
        try
        {
            return hasher.VerifyHashedPassword(u, u.PasswordHash, password) == PasswordVerificationResult.Failed ? null : u;
        }
        catch (FormatException)
        {
            return null;
        }
    });
    public async Task<bool> RegisterAsync(string name, string email, string password)
    {
        name = name.Trim();
        email = email.Trim();
        if (name.Length is < 2 or > 80 || email.Length > 200 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || password.Length is < 8 or > 100)
            throw new BusinessException("Thông tin tài khoản chưa hợp lệ.");
        var u = new AppUser
        {
            Name = name,
            Email = email
        };
        var hash = hasher.HashPassword(u, password);
        try
        {
            return await db.ReadAsync(async s => await s.ExecuteAsync("INSERT dbo.TaiKhoan(TenHienThi,Email,MatKhauHash) VALUES(@name,@email,@hash)", ("@name", name), ("@email", email), ("@hash", hash)) > 0);
        }
        catch (SqlException e)when (e.Number is 2601 or 2627)
        {
            return false;
        }
    }

    public Task<List<string>> GenresAsync() => db.ReadAsync(async s => (await s.QueryAsync("SELECT TenTheLoai FROM dbo.TheLoai ORDER BY MaTheLoai")).Select(r => r.Text("TenTheLoai")).ToList());
    public Task<ForumData> ReadAsync(int userId = 0, bool management = false) => db.ReadAsync(async s =>
    {
        // ponytail: reuse the existing Razor view models; move catalog pagination into SQL when the collection grows beyond a few thousand books.
        var sets = await s.BatchAsync("""
   DECLARE @role varchar(20)=(SELECT MaVaiTro FROM dbo.TaiKhoan WHERE MaTaiKhoan=@actor AND BiKhoa=0);
   DECLARE @review bit=CASE WHEN @manage=1 AND @role IN('Admin','Moderator') THEN 1 ELSE 0 END;
   SELECT MaSach,TenSach,TacGia,GioiThieu,LoaiTruyen,TrangThaiSangTac,MaNguoiDang,
     ChoDuyet,BiAn,LuotDoc,NgayTao,NgayCapNhat,
     CAST(CASE WHEN HinhAnhBia IS NULL THEN 0 ELSE 1 END AS bit) AS CoAnhBia
   INTO #Books FROM dbo.Sach WHERE (ChoDuyet=0 AND BiAn=0 AND @manage=0)
     OR (@manage=1 AND (@review=1 OR (@role='Contributor' AND MaNguoiDang=@actor)));
   SELECT c.* INTO #Chapters FROM dbo.Chuong c JOIN #Books b ON b.MaSach=c.MaSach;
   SELECT p.* INTO #Posts FROM dbo.BaiThaoLuan p WHERE p.BiAn=0 OR @review=1;
   SELECT * FROM #Books ORDER BY NgayTao DESC,MaSach DESC;
   SELECT st.MaSach,t.TenTheLoai FROM dbo.SachTheLoai st JOIN dbo.TheLoai t ON t.MaTheLoai=st.MaTheLoai JOIN #Books b ON b.MaSach=st.MaSach ORDER BY st.ThuTu;
   SELECT * FROM #Chapters ORDER BY SoChuong;
   SELECT n.* FROM dbo.NoiDungChuong n JOIN #Chapters c ON c.MaChuong=n.MaChuong ORDER BY n.ThuTu;
   SELECT dg.* FROM dbo.DanhGia dg JOIN #Books b ON b.MaSach=dg.MaSach;
   SELECT MaTaiKhoan,TenHienThi,Email,MaVaiTro,BiKhoa,DauBaoMat FROM dbo.TaiKhoan WHERE MaTaiKhoan=@actor OR (@manage=1 AND @role='Admin');
   SELECT * FROM dbo.TuSach WHERE MaTaiKhoan=@actor;
   SELECT l.*,c.SoChuong FROM dbo.LichSuDoc l JOIN dbo.Chuong c ON c.MaChuong=l.MaChuong WHERE l.MaTaiKhoan=@actor ORDER BY l.LanDocCuoi DESC;
   SELECT p.*,u.TenHienThi FROM #Posts p JOIN dbo.TaiKhoan u ON u.MaTaiKhoan=p.MaTaiKhoan ORDER BY p.NgayTao DESC;
   SELECT bt.MaBaiThaoLuan,t.TenTag FROM dbo.BaiThaoLuanTag bt JOIN dbo.Tag t ON t.MaTag=bt.MaTag JOIN #Posts p ON p.MaBaiThaoLuan=bt.MaBaiThaoLuan ORDER BY bt.ThuTu;
   IF @review=1 SELECT c.*,u.TenHienThi FROM dbo.BinhLuan c JOIN dbo.TaiKhoan u ON u.MaTaiKhoan=c.MaTaiKhoan ORDER BY c.NgayTao;
   ELSE SELECT * FROM dbo.vw_BinhLuanCongKhai ORDER BY MaGoc,NgayTao,MaBinhLuan OPTION(MAXRECURSION 0);
   SELECT * FROM dbo.BaoCao WHERE @review=1 ORDER BY NgayBaoCao DESC;
   SELECT TenTheLoai FROM dbo.TheLoai ORDER BY MaTheLoai;
   DROP TABLE #Posts; DROP TABLE #Chapters; DROP TABLE #Books;
   """, ("@actor", userId), ("@manage", management));
        var d = new ForumData();
        d.Stories = sets[0].Select(r => new Story { Id = r.Int("MaSach"), Title = r.Text("TenSach"), Author = r.Text("TacGia"), HasCover = r.Bool("CoAnhBia"), Description = r.Text("GioiThieu"), Kind = (StoryKind)r.Int("LoaiTruyen"), Status = r.Int("TrangThaiSangTac") == 0 ? "Đang ra" : "Hoàn thành", OwnerId = r.Int("MaNguoiDang"), PendingApproval = r.Bool("ChoDuyet"), Hidden = r.Bool("BiAn"), Views = r.Long("LuotDoc"), CreatedAt = r.Date("NgayTao") }).ToList();
        var books = d.Stories.ToDictionary(b => b.Id);
        foreach (var r in sets[1])
            books[r.Int("MaSach")].Genres.Add(r.Text("TenTheLoai"));
        foreach (var r in sets[2])
            books[r.Int("MaSach")].Chapters.Add(new Chapter { Id = r.Int("MaChuong"), Number = r.Int("SoChuong"), Title = r.Text("TieuDe") });
        var chapters = d.Stories.SelectMany(b => b.Chapters).ToDictionary(c => c.Id);
        foreach (var r in sets[3])
            chapters[r.Int("MaChuong")].Files.Add(FileRow(r));
        foreach (var r in sets[4])
            books[r.Int("MaSach")].Ratings[r.Int("MaTaiKhoan")] = r.Int("SoSao");
        d.Users = sets[5].Select(User).ToList();
        var current = d.Users.FirstOrDefault(u => u.Id == userId);
        if (current != null)
        {
            foreach (var r in sets[6])
                current.Saved.Add(r.Int("MaSach"));
            foreach (var r in sets[7])
                current.History[r.Int("MaSach")] = r.Int("MaChuong");
        }

        d.Posts = sets[8].Select(r => new Discussion { Id = r.Int("MaBaiThaoLuan"), UserId = r.Int("MaTaiKhoan"), Author = r.Text("TenHienThi"), Title = r.Text("TieuDe"), Body = r.Text("NoiDung"), Hidden = r.Bool("BiAn"), CreatedAt = r.Date("NgayTao") }).ToList();
        var posts = d.Posts.ToDictionary(p => p.Id);
        foreach (var r in sets[9])
            posts[r.Int("MaBaiThaoLuan")].Tags.Add(r.Text("TenTag"));
        d.Comments = sets[10].Select(r => new Comment { Id = r.Int("MaBinhLuan"), StoryId = r.OptionalInt("MaSach"), ChapterId = r.OptionalInt("MaChuong"), PostId = r.OptionalInt("MaBaiThaoLuan"), ParentId = r.OptionalInt("MaBinhLuanCha"), UserId = r.Int("MaTaiKhoan"), Author = r.Text("TenHienThi"), Text = r.Text("NoiDung"), Hidden = r.Bool("BiAn"), CreatedAt = r.Date("NgayTao") }).ToList();
        d.Reports = sets[11].Select(r => new ContentReport { Id = r.Int("MaBaoCao"), Kind = r.OptionalInt("MaSach").HasValue ? "book" : r.OptionalInt("MaBaiThaoLuan").HasValue ? "post" : "comment", TargetId = r.OptionalInt("MaSach") ?? r.OptionalInt("MaBaiThaoLuan") ?? r.Int("MaBinhLuan"), TargetTitle = r.Text("NoiDungBiBaoCao"), UserId = r.Int("MaNguoiBaoCao"), Reason = r.Text("LyDo"), Resolved = r.Bool("DaXuLy"), Resolution = r.Text("KetQuaXuLy"), ResolverId = r.OptionalInt("MaNguoiXuLy"), CreatedAt = r.Date("NgayBaoCao"), ResolvedAt = r.OptionalDate("NgayXuLy") }).ToList();
        d.Genres = sets[12].Select(r => r.Text("TenTheLoai")).ToList();
        return d;
    });
    static async Task<AppUser> RequireAsync(SqlSession s, Actor actor, params string[] roles)
    {
        var rows = await s.QueryAsync("SELECT MaTaiKhoan,TenHienThi,Email,MaVaiTro,BiKhoa,DauBaoMat FROM dbo.TaiKhoan WITH(HOLDLOCK) WHERE MaTaiKhoan=@id", ("@id", actor.Id));
        if (rows.Count == 0)
            throw new BusinessException("Phiên đăng nhập không còn hợp lệ.", 403);
        var u = User(rows[0]);
        if (u.Locked || !u.Stamp.Equals(actor.Stamp, StringComparison.OrdinalIgnoreCase) || roles.Length > 0 && !roles.Contains(u.Role))
            throw new BusinessException("Bạn không có quyền thực hiện thao tác này. Hãy đăng nhập lại.", 403);
        return u;
    }

    static void Valid(string? text, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > max)
            throw new BusinessException($"{label} bắt buộc, tối đa {max} ký tự.");
    }

    public Task<int> SaveBookAsync(Actor actor, EditBookInput input, CoverImage? cover = null) => db.TransactionAsync(async s =>
    {
        var user = await RequireAsync(s, actor, Roles.Admin, Roles.Contributor);
        Valid(input.Title, 160, "Tên sách");
        Valid(input.Author, 100, "Tác giả");
        Valid(input.Description, 3000, "Giới thiệu");
        if (!Enum.IsDefined(input.Kind) || input.Status is not ("Đang ra" or "Hoàn thành"))
            throw new BusinessException("Loại hoặc trạng thái sách không hợp lệ.");
        if (input.RemoveCover && cover != null)
            throw new BusinessException("Chọn tải ảnh bìa mới hoặc xóa bìa hiện tại.");
        if (cover != null && (cover.Data.Length is < 1 or > ChapterFiles.MaxImageBytes || cover.ContentType is not ("image/png" or "image/jpeg" or "image/webp")))
            throw new BusinessException("Ảnh bìa không hợp lệ.");
        var genres = input.Genres.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (genres.Count == 0)
            throw new BusinessException("Chọn ít nhất một thể loại.");
        var genreIds = new List<int>();
        foreach (var name in genres)
        {
            var rows = await s.QueryAsync("SELECT MaTheLoai FROM dbo.TheLoai WITH(HOLDLOCK) WHERE TenTheLoai=@name", ("@name", name));
            if (rows.Count == 0)
                throw new BusinessException("Thể loại đã thay đổi. Hãy tải lại trang.");
            genreIds.Add(rows[0].Int("MaTheLoai"));
        }

        int id = input.Id;
        if (id != 0)
        {
            var rows = await s.QueryAsync("SELECT * FROM dbo.Sach WITH(UPDLOCK,HOLDLOCK) WHERE MaSach=@id", ("@id", id));
            if (rows.Count == 0)
                throw new BusinessException("Sách không tồn tại.", 404);
            var b = rows[0];
            if (user.Role != Roles.Admin && b.Int("MaNguoiDang") != actor.Id)
                throw new BusinessException("Không phải sách của bạn.", 403);
            if (b.Int("LoaiTruyen") != (int)input.Kind && (await s.QueryAsync("SELECT TOP(1) MaChuong FROM dbo.Chuong WHERE MaSach=@id", ("@id", id))).Count > 0)
                throw new BusinessException("Không thể đổi loại sách đã có chương.");
            await s.ExecuteAsync("UPDATE dbo.Sach SET TenSach=@title,TacGia=@author,GioiThieu=@description,LoaiTruyen=@kind,TrangThaiSangTac=@status,HinhAnhBia=CASE WHEN @removeCover=1 THEN NULL WHEN @cover IS NOT NULL THEN @cover ELSE HinhAnhBia END,KieuAnhBia=CASE WHEN @removeCover=1 THEN NULL WHEN @cover IS NOT NULL THEN @mime ELSE KieuAnhBia END,ChoDuyet=CASE WHEN @admin=1 THEN ChoDuyet ELSE 1 END,NgayCapNhat=SYSUTCDATETIME() WHERE MaSach=@id", ("@title", input.Title.Trim()), ("@author", input.Author.Trim()), ("@description", input.Description.Trim()), ("@kind", (byte)input.Kind), ("@status", input.Status == "Đang ra" ? 0 : 1), ("@admin", user.Role == Roles.Admin), ("@id", id), ("@removeCover", input.RemoveCover), ("@cover", cover?.Data), ("@mime", cover?.ContentType));
        }
        else
        {
            var rows = await s.QueryAsync("INSERT dbo.Sach(TenSach,TacGia,GioiThieu,LoaiTruyen,TrangThaiSangTac,MaNguoiDang,ChoDuyet,HinhAnhBia,KieuAnhBia) OUTPUT INSERTED.MaSach VALUES(@title,@author,@description,@kind,@status,@actor,@pending,@cover,@mime)", ("@title", input.Title.Trim()), ("@author", input.Author.Trim()), ("@description", input.Description.Trim()), ("@kind", (byte)input.Kind), ("@status", input.Status == "Đang ra" ? 0 : 1), ("@actor", actor.Id), ("@pending", user.Role != Roles.Admin), ("@cover", cover?.Data), ("@mime", cover?.ContentType));
            id = rows[0].Int("MaSach");
        }

        await s.ExecuteAsync("DELETE dbo.SachTheLoai WHERE MaSach=@id", ("@id", id));
        for (int i = 0; i < genreIds.Count; i++)
            await s.ExecuteAsync("INSERT dbo.SachTheLoai(MaSach,MaTheLoai,ThuTu) VALUES(@id,@genre,@order)", ("@id", id), ("@genre", genreIds[i]), ("@order", i + 1));
        return id;
    });
    public async Task EnsureBookOwnerAsync(Actor actor, int id)
    {
        await db.ReadAsync(async s =>
        {
            var u = await RequireAsync(s, actor, Roles.Admin, Roles.Contributor);
            var b = (await s.QueryAsync("SELECT MaNguoiDang FROM dbo.Sach WHERE MaSach=@id", ("@id", id))).FirstOrDefault();
            if (b == null)
                throw new BusinessException("Sách không tồn tại.", 404);
            if (u.Role != Roles.Admin && b.Int("MaNguoiDang") != actor.Id)
                throw new BusinessException("Không phải sách của bạn.", 403);
            return true;
        });
    }

    public Task<int> AddChapterAsync(Actor actor, int id, string title, List<ContentFile> files) => db.ReadAsync(async s =>
    {
        await RequireAsync(s, actor, Roles.Admin, Roles.Contributor);
        Valid(title, 160, "Tên chương");
        if (files.Count == 0)
            throw new BusinessException("Chọn ít nhất một tệp.");
        var table = new DataTable();
        table.Columns.Add("ThuTu", typeof(int));
        table.Columns.Add("DuongDanTep", typeof(string));
        table.Columns.Add("KieuTep", typeof(string));
        table.Columns.Add("TenTepGoc", typeof(string));
        table.Columns.Add("DungLuong", typeof(long));
        for (int i = 0; i < files.Count; i++)
        {
            var f = files[i];
            table.Rows.Add(i + 1, f.Key, f.ContentType, f.OriginalName, f.Size);
        }

        var rows = await s.ProcedureAsync("dbo.usp_ThemChuong", ("@MaNguoiThaoTac", actor.Id), ("@MaSach", id), ("@TieuDe", title.Trim()), ("@Tep", table));
        return rows[0].Int("MaChuong");
    });
    public Task<bool> FilesCommittedAsync(List<ContentFile> files) => db.ReadAsync(async s =>
    {
        foreach (var f in files)
            if ((await s.QueryAsync("SELECT MaNoiDung FROM dbo.NoiDungChuong WHERE DuongDanTep=@key", ("@key", f.Key))).Count == 0)
                return false;
        return true;
    });
    public Task RecordReadAsync(int id, int number, int userId, Guid? guest) => db.ReadAsync(async s =>
    {
        await s.ProcedureAsync("dbo.usp_DocChuong", ("@MaSach", id), ("@SoChuong", number), ("@MaTaiKhoan", userId == 0 ? null : (object)userId), ("@MaKhach", guest));
        return true;
    });
    public Task<CoverImage?> CoverAsync(int id, int userId) => db.ReadAsync(async s =>
    {
        var rows = await s.QueryAsync("""
            SELECT b.HinhAnhBia,b.KieuAnhBia FROM dbo.Sach b
            WHERE b.MaSach=@id AND b.HinhAnhBia IS NOT NULL AND
            ((b.ChoDuyet=0 AND b.BiAn=0) OR EXISTS(
                SELECT 1 FROM dbo.TaiKhoan u WHERE u.MaTaiKhoan=@actor AND u.BiKhoa=0
                AND (u.MaVaiTro IN('Admin','Moderator') OR (u.MaVaiTro='Contributor' AND b.MaNguoiDang=u.MaTaiKhoan))));
            """, ("@id", id), ("@actor", userId));
        return rows.Count == 0 ? null : new CoverImage((byte[])rows[0]["HinhAnhBia"]!, rows[0].Text("KieuAnhBia"));
    });
    public Task<ContentFile?> FileAsync(int id, int userId) => db.ReadAsync(async s =>
    {
        var r = await s.ProcedureAsync("dbo.usp_LayTepNoiDung", ("@MaNoiDung", id), ("@MaTaiKhoan", userId == 0 ? null : (object)userId));
        return r.Count == 0 ? null : FileRow(r[0]);
    });
    public Task<Dictionary<RankingPeriod, List<RankedStory>>> RankingsAsync(List<Story> books) => db.ReadAsync(async s =>
    {
        var result = new Dictionary<RankingPeriod, List<RankedStory>>();
        foreach (var period in Enum.GetValues<RankingPeriod>())
        {
            var rows = await s.ProcedureAsync("dbo.usp_BangXepHang", ("@Ky", period.ToString()));
            result[period] = rows.Where(r => books.Any(b => b.Id == r.Int("MaSach"))).Select(r => new RankedStory(books.First(b => b.Id == r.Int("MaSach")), r.Long("LuotDocTrongKy"))).ToList();
        }

        return result;
    });
    public Task SaveAsync(Actor actor, int id, bool saved) => db.ReadAsync(async s =>
    {
        await RequireAsync(s, actor);
        await s.ProcedureAsync("dbo.usp_LuuTuSach", ("@MaTaiKhoan", actor.Id), ("@MaSach", id), ("@Luu", saved));
        return true;
    });
    public Task RateAsync(Actor actor, int id, int value) => db.ReadAsync(async s =>
    {
        await RequireAsync(s, actor);
        await s.ProcedureAsync("dbo.usp_LuuDanhGia", ("@MaTaiKhoan", actor.Id), ("@MaSach", id), ("@SoSao", value));
        return true;
    });
    public Task<int> CommentAsync(Actor actor, int? story, int? chapter, int? post, int? parent, string text) => db.ReadAsync(async s =>
    {
        await RequireAsync(s, actor);
        Valid(text, 2000, "Bình luận");
        if (new[]
        {
            story,
            chapter,
            post
        }.Count(x => x.HasValue) != 1)
            throw new BusinessException("Bình luận phải có đúng một đích.");
        var rows = await s.ProcedureAsync("dbo.usp_ThemBinhLuan", ("@MaTaiKhoan", actor.Id), ("@NoiDung", text.Trim()), ("@MaSach", story), ("@MaChuong", chapter), ("@MaBaiThaoLuan", post), ("@MaBinhLuanCha", parent));
        return rows[0].Int("MaBinhLuan");
    });
    public Task<int> CreateDiscussionAsync(Actor actor, CreateDiscussionInput input) => db.TransactionAsync(async s =>
    {
        await RequireAsync(s, actor);
        Valid(input.Title, 160, "Tiêu đề");
        Valid(input.Body, 5000, "Nội dung");
        var tags = (input.Tags ?? "").Split(',').Select(t => t.Trim()).Where(t => t != "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (tags.Count > 5 || tags.Any(t => t.Length > 30))
            throw new BusinessException("Tối đa 5 tag, mỗi tag 30 ký tự.");
        var rows = await s.QueryAsync("INSERT dbo.BaiThaoLuan(MaTaiKhoan,TieuDe,NoiDung) OUTPUT INSERTED.MaBaiThaoLuan VALUES(@actor,@title,@text)", ("@actor", actor.Id), ("@title", input.Title.Trim()), ("@text", input.Body.Trim()));
        int id = rows[0].Int("MaBaiThaoLuan");
        for (int i = 0; i < tags.Count; i++)
            await s.ExecuteAsync("IF NOT EXISTS(SELECT 1 FROM dbo.Tag WITH(UPDLOCK,HOLDLOCK) WHERE TenTag=@tag) INSERT dbo.Tag(TenTag) VALUES(@tag); INSERT dbo.BaiThaoLuanTag(MaBaiThaoLuan,MaTag,ThuTu) SELECT @id,MaTag,@order FROM dbo.Tag WHERE TenTag=@tag", ("@tag", tags[i]), ("@id", id), ("@order", i + 1));
        return id;
    });
    public Task ReportAsync(Actor actor, string kind, int id, string reason) => db.TransactionAsync(async s =>
    {
        await RequireAsync(s, actor);
        Valid(reason, 1000, "Lý do");
        string sql = kind switch
        {
            "book" => "SELECT TenSach AS Snapshot FROM dbo.Sach WITH(HOLDLOCK) WHERE MaSach=@id AND ChoDuyet=0 AND BiAn=0",
            "post" => "SELECT CONCAT(TieuDe,NCHAR(10),NoiDung) AS Snapshot FROM dbo.BaiThaoLuan WITH(HOLDLOCK) WHERE MaBaiThaoLuan=@id AND BiAn=0",
            "comment" => "SELECT NoiDung AS Snapshot FROM dbo.vw_BinhLuanCongKhai WHERE MaBinhLuan=@id OPTION(MAXRECURSION 0)",
            _ => throw new BusinessException("Loại báo cáo không hợp lệ.")};
        var rows = await s.QueryAsync(sql, ("@id", id));
        if (rows.Count == 0)
            throw new BusinessException("Nội dung không còn công khai.", 404);
        var snapshot = rows[0].Text("Snapshot");
        if (snapshot.Length > 2000)
            snapshot = snapshot[..2000];
        await s.ExecuteAsync("INSERT dbo.BaoCao(MaNguoiBaoCao,MaSach,MaBaiThaoLuan,MaBinhLuan,NoiDungBiBaoCao,LyDo) VALUES(@actor,@book,@post,@comment,@snapshot,@reason)", ("@actor", actor.Id), ("@book", kind == "book" ? (object)id : null), ("@post", kind == "post" ? (object)id : null), ("@comment", kind == "comment" ? (object)id : null), ("@snapshot", snapshot), ("@reason", reason.Trim()));
        return true;
    });
    public Task ModerateAsync(Actor actor, string kind, int id, string action) => db.TransactionAsync(async s =>
    {
        await RequireAsync(s, actor, Roles.Admin, Roles.Moderator);
        if (action is not ("approve" or "hide" or "restore") || action == "approve" && kind != "book")
            throw new BusinessException("Thao tác không hợp lệ.");
        var(table, key) = kind switch
        {
            "book" => ("Sach", "MaSach"),
            "post" => ("BaiThaoLuan", "MaBaiThaoLuan"),
            "comment" => ("BinhLuan", "MaBinhLuan"),
            _ => throw new BusinessException("Loại nội dung không hợp lệ.")};
        var column = action == "approve" ? "ChoDuyet" : "BiAn";
        if (await s.ExecuteAsync($"UPDATE dbo.{table} SET {column}=@value WHERE {key}=@id", ("@value", action == "hide"), ("@id", id)) == 0)
            throw new BusinessException("Nội dung không tồn tại.", 404);
        return true;
    });
    public Task<bool> ResolveAsync(Actor actor, int id, string resolution) => db.TransactionAsync(async s =>
    {
        await RequireAsync(s, actor, Roles.Admin, Roles.Moderator);
        Valid(resolution, 1000, "Kết quả xử lý");
        return await s.ExecuteAsync("UPDATE dbo.BaoCao SET DaXuLy=1,MaNguoiXuLy=@actor,KetQuaXuLy=@text,NgayXuLy=SYSUTCDATETIME() WHERE MaBaoCao=@id AND DaXuLy=0", ("@actor", actor.Id), ("@text", resolution.Trim()), ("@id", id)) > 0;
    });
    public Task GenreAsync(Actor actor, string action, string name, string? newName = null) => db.TransactionAsync(async s =>
    {
        await RequireAsync(s, actor, Roles.Admin);
        Valid(name, 60, "Thể loại");
        name = name.Trim();
        if (action == "add")
            await s.ExecuteAsync("INSERT dbo.TheLoai(TenTheLoai) VALUES(@name)", ("@name", name));
        else if (action == "rename")
        {
            Valid(newName, 60, "Tên mới");
            if (await s.ExecuteAsync("UPDATE dbo.TheLoai SET TenTheLoai=@new WHERE TenTheLoai=@name", ("@new", newName!.Trim()), ("@name", name)) == 0)
                throw new BusinessException("Thể loại không tồn tại.", 404);
        }
        else if (action == "delete")
        {
            var used = await s.QueryAsync("SELECT st.MaSach FROM dbo.SachTheLoai st JOIN dbo.TheLoai t ON t.MaTheLoai=st.MaTheLoai WHERE t.TenTheLoai=@name", ("@name", name));
            if (used.Count > 0)
                throw new BusinessException("Không thể xóa thể loại đang có sách.");
            if (await s.ExecuteAsync("DELETE dbo.TheLoai WHERE TenTheLoai=@name", ("@name", name)) == 0)
                throw new BusinessException("Thể loại không tồn tại.", 404);
        }
        else
            throw new BusinessException("Thao tác không hợp lệ.");
        return true;
    });
    public Task UserRoleAsync(Actor actor, int id, string role) => db.TransactionAsync(async s =>
    {
        await RequireAsync(s, actor, Roles.Admin);
        if (!Roles.All.Contains(role) || id == actor.Id && role != Roles.Admin)
            throw new BusinessException("Không thể tự bỏ quyền quản trị hoặc đặt vai trò không hợp lệ.");
        if (await s.ExecuteAsync("UPDATE dbo.TaiKhoan SET MaVaiTro=@role,DauBaoMat=NEWID() WHERE MaTaiKhoan=@id AND MaVaiTro<>@role", ("@role", role), ("@id", id)) == 0 && (await s.QueryAsync("SELECT MaTaiKhoan FROM dbo.TaiKhoan WHERE MaTaiKhoan=@id", ("@id", id))).Count == 0)
            throw new BusinessException("Tài khoản không tồn tại.", 404);
        return true;
    });
    public Task LockUserAsync(Actor actor, int id, bool locked) => db.TransactionAsync(async s =>
    {
        await RequireAsync(s, actor, Roles.Admin);
        if (id == actor.Id)
            throw new BusinessException("Không thể tự khóa tài khoản.");
        if (await s.ExecuteAsync("UPDATE dbo.TaiKhoan SET BiKhoa=@locked,DauBaoMat=NEWID() WHERE MaTaiKhoan=@id AND BiKhoa<>@locked", ("@locked", locked), ("@id", id)) == 0 && (await s.QueryAsync("SELECT MaTaiKhoan FROM dbo.TaiKhoan WHERE MaTaiKhoan=@id", ("@id", id))).Count == 0)
            throw new BusinessException("Tài khoản không tồn tại.", 404);
        return true;
    });
}
