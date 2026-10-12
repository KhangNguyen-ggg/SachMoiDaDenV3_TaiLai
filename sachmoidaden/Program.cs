using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using sachmoidaden.Data;
using sachmoidaden.Models;
using sachmoidaden.Services;

if (args.Contains("--create-admin") && args.Contains("--seed-demo"))
    throw new ArgumentException("Chỉ chọn một lệnh: --create-admin hoặc --seed-demo.");
var builder = WebApplication.CreateBuilder(args.Where(a => a is not ("--create-admin" or "--seed-demo")).ToArray());
builder.Services.AddSingleton<SqlDatabase>();
builder.Services.AddScoped<LibraryService>();
builder.Services.AddSingleton<ChapterFiles>();
builder.Services.AddScoped<DemoSeeder>();
var keys = Path.GetFullPath(builder.Configuration["Storage:Keys"] ?? "App_Data/Keys", builder.Environment.ContentRootPath);
Directory.CreateDirectory(keys);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys)).SetApplicationName("SachMoiDaDen");
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = ChapterFiles.MaxRequestBytes);
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.Filters.Add<BusinessErrorFilter>();
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Denied";
    o.ExpireTimeSpan = TimeSpan.FromHours(4);
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Events.OnValidatePrincipal = async c =>
    {
        int.TryParse(c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out int id);
        var u = await c.HttpContext.RequestServices.GetRequiredService<LibraryService>().FindUserAsync(id);
        if (u == null || u.Locked || !u.Stamp.Equals(c.Principal?.FindFirstValue("stamp"), StringComparison.OrdinalIgnoreCase))
        {
            c.RejectPrincipal();
            await c.HttpContext.SignOutAsync();
        }
    };
});
var app = builder.Build();
// An explicit local command creates the first Admin. Normal registration always creates Member.
if (args.Contains("--create-admin"))
{
    var email = Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_EMAIL")?.Trim();
    var name = Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_NAME")?.Trim() ?? "Quản trị viên";
    var password = Environment.GetEnvironmentVariable("BOOTSTRAP_ADMIN_PASSWORD");
    if (email == null || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) || email.Length > 200 || name.Length is < 2 or > 80 || password == null || password.Length is < 8 or > 100)
        throw new InvalidOperationException("Đặt BOOTSTRAP_ADMIN_EMAIL và BOOTSTRAP_ADMIN_PASSWORD (8–100 ký tự); tên 2–80 ký tự.");
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SqlDatabase>();
    var user = new AppUser
    {
        Name = name,
        Email = email,
        Role = Roles.Admin
    };
    var hash = new PasswordHasher<AppUser>().HashPassword(user, password);
    await db.TransactionAsync(async s =>
    {
        if ((await s.QueryAsync("SELECT TOP(1) MaTaiKhoan FROM dbo.TaiKhoan WITH(UPDLOCK,HOLDLOCK) WHERE MaVaiTro='Admin'")).Count > 0)
            throw new InvalidOperationException("Đã có Admin; dùng giao diện Tài khoản để cấp quyền.");
        await s.ExecuteAsync("INSERT dbo.TaiKhoan(TenHienThi,Email,MatKhauHash,MaVaiTro) VALUES(@name,@email,@hash,'Admin')", ("@name", name), ("@email", email), ("@hash", hash));
        return true;
    });
    Console.WriteLine("Đã tạo Admin đầu tiên. Chạy lại ứng dụng không có --create-admin.");
    return;
}

if (args.Contains("--seed-demo") || (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("DemoData:SeedOnStartup")))
{
    using var scope = app.Services.CreateScope();
    bool created = await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync();
    Console.WriteLine(created ? "Đã tạo 8 tài khoản, 48 truyện và 288 chương mẫu. Xem docs/DU_LIEU_MAU.md để đăng nhập." : "Bộ dữ liệu mẫu đã tồn tại; giữ nguyên tài khoản và dữ liệu.");
    if (args.Contains("--seed-demo"))
        return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    if (context.Request.Path.StartsWithSegments("/Story") || context.Request.Path.StartsWithSegments("/Management") || context.Request.Path.StartsWithSegments("/User"))
        context.Response.Headers.CacheControl = "no-store, private";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
