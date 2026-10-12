using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using sachmoidaden.Models;
using sachmoidaden.Services;

namespace sachmoidaden.Controllers;
public class AccountController(LibraryService library) : ForumController(library)
{
    [HttpGet]
    public IActionResult Login(string? returnUrl) => View(new LoginInput { ReturnUrl = returnUrl });
    [HttpPost]
    public async Task<IActionResult> Login(LoginInput input)
    {
        input.Email = (input.Email ?? "").Trim();
        ModelState.Clear();
        TryValidateModel(input);
        if (ModelState.IsValid)
        {
            var u = await Library.ValidateAsync(input.Email, input.Password);
            if (u != null)
            {
                await HttpContext.SignInAsync(LibraryService.Principal(u));
                return LocalRedirect(Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl! : "/");
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng, hoặc tài khoản đã bị khóa.");
        }

        return View(input);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterInput());
    [HttpPost]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        input.Email = (input.Email ?? "").Trim();
        input.Name = (input.Name ?? "").Trim();
        ModelState.Clear();
        TryValidateModel(input);
        if (string.IsNullOrEmpty(input.Password) || input.Password.Length < 8)
            ModelState.AddModelError("Password", "Mật khẩu cần ít nhất 8 ký tự.");
        if (ModelState.IsValid)
        {
            if (await Library.RegisterAsync(input.Name, input.Email, input.Password))
                return Notice("Đăng ký thành công. Hãy đăng nhập.", "/Account/Login");
            ModelState.AddModelError("Email", "Email đã được sử dụng.");
        }

        return View(input);
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Denied()
    {
        Response.StatusCode = 403;
        return View();
    }
}
