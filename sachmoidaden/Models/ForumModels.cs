using System.ComponentModel.DataAnnotations;

namespace sachmoidaden.Models;
public static class Roles
{
    public const string Member = "Member", Contributor = "Contributor", Moderator = "Moderator", Admin = "Admin";
    public static readonly string[] All = [Member, Contributor, Moderator, Admin];
    public static string Label(string role) => role switch
    {
        Contributor => "Thành viên đóng góp",
        Moderator => "Kiểm duyệt viên",
        Admin => "Quản trị viên",
        _ => "Thành viên"
    };
}

public class AppUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Member;
    public string Stamp { get; set; } = Guid.NewGuid().ToString();
    public bool Locked { get; set; }
    public HashSet<int> Saved { get; set; } = [];
    public Dictionary<int, int> History { get; set; } = [];
}

public class Discussion
{
    public int Id { get; set; }
    public List<string> Tags { get; set; } = [];
    public int UserId { get; set; }
    public string Author { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool Hidden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ContentReport
{
    public int Id { get; set; }
    public string Kind { get; set; } = "";
    public int TargetId { get; set; }
    public string TargetTitle { get; set; } = "";
    public int UserId { get; set; }
    public string Reason { get; set; } = "";
    public bool Resolved { get; set; }
    public string Resolution { get; set; } = "";
    public int? ResolverId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ForumData
{
    public List<Story> Stories { get; set; } = [];
    public List<AppUser> Users { get; set; } = [];
    public List<Discussion> Posts { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
    public List<ContentReport> Reports { get; set; } = [];
    public List<string> Genres { get; set; } = [];
}

public class LoginInput
{
    [Required(ErrorMessage = "Nhập email."), EmailAddress(ErrorMessage = "Email chưa hợp lệ."), StringLength(200)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu."), StringLength(100), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public class RegisterInput : LoginInput
{
    [Required(ErrorMessage = "Nhập tên hiển thị."), StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại chưa khớp."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class EditBookInput
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    public string Title { get; set; } = "";

    [Required, StringLength(100)]
    public string Author { get; set; } = "";
    public List<string> Genres { get; set; } = [];
    public StoryKind Kind { get; set; }

    [Required, StringLength(3000)]
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Đang ra";
}

public record CatalogViewModel(List<Story> Books, List<string> AllGenres, string Q, List<string> SelectedGenres, int Page, int Pages, int Total);
public record StoryViewModel(Story Book, List<Comment> Comments, bool Saved, int MyRating);
public record ReaderViewModel(Story Book, int Number, List<Comment> Comments, List<RenderedFile> Content, bool Preview = false);
public record DiscussionViewModel(Discussion Post, List<Comment> Comments);
public class CreateDiscussionInput
{
    [Required, StringLength(160)]
    public string Title { get; set; } = "";

    [Required, StringLength(5000)]
    public string Body { get; set; } = "";

    [StringLength(500)]
    public string? Tags { get; set; }
}

public record ForumViewModel(List<Discussion> Posts, List<string> Tags, string? Tag, int Page, int Pages);
public record CommentListViewModel(List<Comment> Comments, int? StoryId, int? PostId, int? ChapterId = null);
public record CommentItemViewModel(Comment Comment, string? ReplyAuthor);
public record RankedStory(Story Book, long Reads);
public record HomeViewModel(List<Story> Featured, List<Story> Popular, List<Story> Newest, List<Story> TopRated, List<string> Genres, Dictionary<sachmoidaden.Services.RankingPeriod, List<RankedStory>> Rankings);
public record RenderedFile(ContentFile File, string? Text);
public record Actor(int Id, string Stamp);
