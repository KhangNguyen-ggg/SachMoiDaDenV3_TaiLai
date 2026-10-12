namespace sachmoidaden.Models;
public enum StoryKind
{
    Text,
    Comic
}

public class Story
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public bool HasCover { get; set; }
    public List<string> Genres { get; set; } = [];
    public StoryKind Kind { get; set; }
    public string Genre => string.Join(" · ", Genres);
    public string Status { get; set; } = "Đang ra";
    public string Description { get; set; } = "";
    public int OwnerId { get; set; }
    public long Views { get; set; }
    public bool PendingApproval { get; set; }
    public bool Hidden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Chapter> Chapters { get; set; } = [];
    public Dictionary<int, int> Ratings { get; set; } = [];
    public bool IsPublic => !PendingApproval && !Hidden;
    public string Rating => Ratings.Count == 0 ? "—" : Ratings.Values.Average().ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
}

public class ContentFile
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Key { get; set; } = "";
    public string ContentType { get; set; } = "";
    public string OriginalName { get; set; } = "";
    public long Size { get; set; }
}

public class Chapter
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = "";
    public List<ContentFile> Files { get; set; } = [];
}

public class Comment
{
    public int Id { get; set; }
    public int? StoryId { get; set; }
    public int? ChapterId { get; set; }
    public int? ParentId { get; set; }
    public int? PostId { get; set; }
    public int UserId { get; set; }
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public bool Hidden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
