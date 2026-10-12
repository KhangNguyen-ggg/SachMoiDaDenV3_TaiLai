using System.Globalization;
using System.Text;
using sachmoidaden.Models;

namespace sachmoidaden.Services;
public enum RankingPeriod
{
    Day,
    Week,
    Month
}

public static class ContentQueries
{
    public static string SearchKey(string s) => new string (s.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant().Replace('đ', 'd');
    public static List<Story> SimilarStories(ForumData d, Story story) => d.Stories.Where(b => b.IsPublic && b.Id != story.Id).Select(b => new { Book = b, Score = b.Genres.Intersect(story.Genres).Count() }).Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Book.Ratings.Count == 0 ? 0 : x.Book.Ratings.Values.Average()).ThenBy(x => x.Book.Title, StringComparer.Ordinal).Take(4).Select(x => x.Book).ToList();
    public static List<Discussion> SimilarPosts(ForumData d, Discussion post) => d.Posts.Where(p => p.Id != post.Id && !p.Hidden).Select(p => new { Post = p, Score = p.Tags.Intersect(post.Tags, StringComparer.OrdinalIgnoreCase).Count() }).Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenByDescending(x => x.Post.CreatedAt).ThenBy(x => x.Post.Id).Take(5).Select(x => x.Post).ToList();
    public static bool IsPublic(ForumData d, Comment c)
    {
        var seen = new HashSet<int>();
        while (true)
        {
            if (c.Hidden || !seen.Add(c.Id))
                return false;
            if (new[]
            {
                c.StoryId,
                c.ChapterId,
                c.PostId
            }.Count(x => x.HasValue) != 1)
                return false;
            if (c.PostId.HasValue)
            {
                if (!d.Posts.Any(p => p.Id == c.PostId && !p.Hidden))
                    return false;
            }
            else if (c.ChapterId.HasValue)
            {
                if (!d.Stories.Any(b => b.IsPublic && b.Chapters.Any(ch => ch.Id == c.ChapterId)))
                    return false;
            }
            else if (!d.Stories.Any(b => b.Id == c.StoryId && b.IsPublic))
                return false;
            if (c.ParentId == null)
                return true;
            var parent = d.Comments.FirstOrDefault(x => x.Id == c.ParentId);
            if (parent == null || parent.PostId != c.PostId || parent.StoryId != c.StoryId || parent.ChapterId != c.ChapterId)
                return false;
            c = parent;
        }
    }

    public static List<Comment> VisibleComments(ForumData d, int? storyId, int? postId, int? chapterId = null)
    {
        var visible = d.Comments.Where(c => c.StoryId == storyId && c.PostId == postId && c.ChapterId == chapterId && IsPublic(d, c)).ToList();
        var byId = visible.ToDictionary(c => c.Id);
        Comment Root(Comment c)
        {
            while (c.ParentId.HasValue)
                c = byId[c.ParentId.Value];
            return c;
        }

        return visible.OrderBy(c => Root(c).CreatedAt).ThenBy(c => Root(c).Id).ThenBy(c => c.CreatedAt).ThenBy(c => c.Id).ToList();
    }

    public static string Scope(Comment c) => c.PostId.HasValue ? $"Diễn đàn #{c.PostId}" : c.ChapterId.HasValue ? $"Chương #{c.ChapterId}" : $"Sách #{c.StoryId}";
    public static DateTime Vietnam(DateTime time) => DateTime.SpecifyKind(time, DateTimeKind.Utc).AddHours(7);
}
