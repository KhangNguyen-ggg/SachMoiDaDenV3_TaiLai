using System.Buffers.Binary;
using System.Text;
using sachmoidaden.Models;

namespace sachmoidaden.Services;
public sealed class ChapterFiles
{
    public const int MaxImageBytes = 5 * 1024 * 1024, MaxTextBytes = 2 * 1024 * 1024, MaxPdfBytes = 20 * 1024 * 1024, MaxRequestBytes = 102 * 1024 * 1024;
    readonly string directory;
    static readonly UTF8Encoding Utf8 = new(false, true);
    public ChapterFiles(IWebHostEnvironment environment, IConfiguration configuration)
    {
        directory = Path.GetFullPath(configuration["Storage:Root"] ?? "App_Data/Chapters", environment.ContentRootPath);
        var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        if (directory.Equals(webRoot, StringComparison.OrdinalIgnoreCase) || directory.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Kho chương phải nằm ngoài wwwroot.");
        Directory.CreateDirectory(directory);
    }

    string PathFor(string key)
    {
        if (!Guid.TryParseExact(key, "N", out _))
            throw new InvalidDataException("Khóa tệp không hợp lệ.");
        return Path.Combine(directory, key);
    }

    public async Task<List<ContentFile>> SaveAsync(IReadOnlyList<IFormFile> files, IReadOnlyList<int>? order, CancellationToken token)
    {
        if (files.Count is < 1 or > 50)
            throw new InvalidDataException("Chọn từ 1 đến 50 tệp mỗi chương.");
        order ??= Enumerable.Range(0, files.Count).ToList();
        if (order.Count != files.Count || !order.Order().SequenceEqual(Enumerable.Range(0, files.Count)))
            throw new InvalidDataException("Thứ tự tệp không hợp lệ.");
        var saved = new List<ContentFile>();
        long total = 0, images = 0;
        int imageCount = 0;
        try
        {
            foreach (int index in order)
            {
                var file = files[index];
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                int limit = ext switch
                {
                    ".txt" or ".md" => MaxTextBytes,
                    ".pdf" => MaxPdfBytes,
                    ".png" or ".jpg" or ".jpeg" or ".webp" => MaxImageBytes,
                    _ => throw new InvalidDataException("Chỉ nhận TXT, MD, PDF, JPG, PNG và WebP.")};
                if (file.Length < 1 || file.Length > limit)
                    throw new InvalidDataException("Tệp rỗng hoặc vượt giới hạn: ảnh 5 MiB, TXT/MD 2 MiB, PDF 20 MiB.");
                using var input = file.OpenReadStream();
                using var buffer = new MemoryStream();
                var chunk = new byte[81920];
                int n;
                while ((n = await input.ReadAsync(chunk.AsMemory(), token)) > 0)
                {
                    total += n;
                    if (buffer.Length + n > limit || total > 100L * 1024 * 1024)
                        throw new InvalidDataException("Vượt giới hạn dung lượng tệp hoặc 100 MiB/chương.");
                    await buffer.WriteAsync(chunk.AsMemory(0, n), token);
                }

                var bytes = buffer.ToArray();
                if (bytes.Length != file.Length)
                    throw new InvalidDataException("Dung lượng tệp không khớp.");
                var mime = Detect(bytes, ext) ?? throw new InvalidDataException("Định dạng thật của tệp không hợp lệ hoặc văn bản chưa phải UTF-8.");
                if (mime.StartsWith("image/"))
                {
                    imageCount++;
                    images += bytes.Length;
                    if (imageCount > 50 || images > 50L * 1024 * 1024)
                        throw new InvalidDataException("Tối đa 50 ảnh và tổng 50 MiB ảnh/chương.");
                }

                var original = Path.GetFileName(file.FileName.Replace('\\', '/'));
                if (original.Length > 255)
                    original = original[..255];
                var f = new ContentFile
                {
                    Key = Guid.NewGuid().ToString("N"),
                    Order = saved.Count + 1,
                    ContentType = mime,
                    OriginalName = original,
                    Size = bytes.Length
                };
                saved.Add(f);
                await System.IO.File.WriteAllBytesAsync(PathFor(f.Key), bytes, token);
            }

            return saved;
        }
        catch
        {
            Delete(saved);
            throw;
        }
    }

    public async Task<List<ContentFile>> SaveTextAsync(string content, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 100000)
            throw new InvalidDataException("Nhập từ 1 đến 100.000 ký tự cho nội dung chương.");
        var bytes = Utf8.GetBytes(content.Trim());
        var file = new ContentFile
        {
            Key = Guid.NewGuid().ToString("N"),
            Order = 1,
            ContentType = "text/plain",
            OriginalName = "chuong.txt",
            Size = bytes.Length
        };
        try
        {
            await System.IO.File.WriteAllBytesAsync(PathFor(file.Key), bytes, token);
            return[file];
        }
        catch
        {
            Delete([file]);
            throw;
        }
    }

    static string? Detect(byte[] b, string ext)
    {
        if (ext is ".txt" or ".md")
        {
            try
            {
                var text = Utf8.GetString(b).TrimStart('\uFEFF');
                if (string.IsNullOrWhiteSpace(text) || text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
                    return null;
                return ext == ".md" ? "text/markdown" : "text/plain";
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }

        if (ext == ".pdf" && b.Length >= 12 && b.AsSpan(0, 5).SequenceEqual("%PDF-"u8) && Encoding.ASCII.GetString(b, Math.Max(0, b.Length - 1024), Math.Min(1024, b.Length)).Contains("%%EOF", StringComparison.Ordinal))
            return "application/pdf";
        if (ext == ".png" && b.Length >= 33 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && b.AsSpan(12, 4).SequenceEqual("IHDR"u8) && BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(16, 4))is> 0 and <= 30000 && BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(20, 4))is> 0 and <= 30000)
            return "image/png";
        if ((ext is ".jpg" or ".jpeg") && b.Length >= 16 && b[0] == 255 && b[1] == 216 && b[2] == 255 && b[^2] == 255 && b[^1] == 217)
            return "image/jpeg";
        if (ext == ".webp" && b.Length >= 30 && b.AsSpan(0, 4).SequenceEqual("RIFF"u8) && b.AsSpan(8, 4).SequenceEqual("WEBP"u8) && BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(4, 4)) == b.Length - 8 && (b.AsSpan(12, 4).SequenceEqual("VP8 "u8) || b.AsSpan(12, 4).SequenceEqual("VP8L"u8) || b.AsSpan(12, 4).SequenceEqual("VP8X"u8)))
            return "image/webp";
        return null;
    }

    public async Task<List<RenderedFile>> PrepareAsync(Chapter chapter, CancellationToken token)
    {
        if (chapter.Files.Count == 0)
            throw new InvalidDataException("Chương chưa có tệp nội dung.");
        var result = new List<RenderedFile>();
        foreach (var f in chapter.Files.OrderBy(f => f.Order))
        {
            using var stream = Open(f);
            if (stream == null)
                throw new InvalidDataException($"Tệp {f.OriginalName} đang thiếu hoặc sai dung lượng. Chưa ghi nhận lượt đọc.");
            string? text = null;
            if (f.ContentType is "text/plain" or "text/markdown")
            {
                using var reader = new StreamReader(stream, Utf8, true);
                text = (await reader.ReadToEndAsync(token)).TrimStart('\uFEFF');
            }
            // Read the entire file to detect I/O failures before recording a read, including image/PDF files.
            else
                await stream.CopyToAsync(Stream.Null, token);
            result.Add(new RenderedFile(f, text));
        }

        return result;
    }

    public FileStream? Open(ContentFile file)
    {
        try
        {
            var stream = new FileStream(PathFor(file.Key), FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length == file.Size)
                return stream;
            stream.Dispose();
            return null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Delete(IEnumerable<ContentFile> files)
    {
        foreach (var f in files)
            System.IO.File.Delete(PathFor(f.Key));
    }
}
