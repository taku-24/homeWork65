using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyChat.Models;
using MyChat.Services;
using MyChat.ViewModels;

namespace MyChat.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;
    private readonly IWebHostEnvironment _env;
    private readonly EmailService _email;
    
    public AccountController(UserManager<User> users, SignInManager<User> signIn, IWebHostEnvironment env, EmailService email)
    {
        _users = users;
        _signIn = signIn;
        _env = env;
        _email = email;
    }
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid) return View(model);

        var age = (int)((DateTime.UtcNow - model.BirthDate).TotalDays / 365.25);
        if (age < 18)
        {
            ModelState.AddModelError(nameof(model.BirthDate), "Регистрация доступна только с 18 лет.");
            return View(model);
        }

        var userByName = await _users.FindByNameAsync(model.UserName);
        var userByEmail = await _users.FindByEmailAsync(model.Email);
        if (userByName != null) ModelState.AddModelError(nameof(model.UserName), "Такой логин уже существует.");
        if (userByEmail != null) ModelState.AddModelError(nameof(model.Email), "Такой email уже существует.");
        if (!ModelState.IsValid) return View(model);

        var newUser = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            BirthDate = model.BirthDate
        };

        if (model.Avatar != null && model.Avatar.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "images", "avatars");
            Directory.CreateDirectory(dir);
            var file = Guid.NewGuid() + Path.GetExtension(model.Avatar.FileName);
            var path = Path.Combine(dir, file);
            using var fs = System.IO.File.Create(path);
            await model.Avatar.CopyToAsync(fs);
            newUser.AvatarPath = "/images/avatars/" + file;
        }
        else newUser.AvatarPath = "/images/avatars/default.png";

        var result = await _users.CreateAsync(newUser, model.Password);
        if (result.Succeeded)
        {
            await _users.AddToRoleAsync(newUser, "user");
            
            var profileUrl = Url.Action("Profile", "Users", new { id = newUser.Id }, Request.Scheme);
            var html = $"""
                <h3>Добро пожаловать, {newUser.UserName}!</h3>
                <p>Ваш логин: <b>{newUser.UserName}</b></p>
                <p>Ссылка на профиль: <a href="{profileUrl}">{profileUrl}</a></p>
                """;
            await _email.SendAsync(newUser.Email!, "Добро пожаловать в MyChat", html);
            

            await _signIn.SignInAsync(newUser, false);
            return RedirectToAction("Index", "Chat");
        }

        foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
        return View(model);
    }


    [HttpGet] public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm m, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(m);

        User? u = m.UserNameOrEmail.Contains('@')
            ? await _users.FindByEmailAsync(m.UserNameOrEmail)
            : await _users.FindByNameAsync(m.UserNameOrEmail);

        if (u == null)
        {
            ModelState.AddModelError("", "Неверный логин или пароль.");
            return View(m);
        }

        var res = await _signIn.PasswordSignInAsync(u, m.Password, m.RememberMe, lockoutOnFailure: true);
        if (res.Succeeded) return Redirect(returnUrl ?? Url.Action("Index", "Chat")!);

        ModelState.AddModelError("", res.IsLockedOut ? "Аккаунт заблокирован." : "Неверный логин или пароль.");
        return View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Chat");
    }
    
    
    
    
}
