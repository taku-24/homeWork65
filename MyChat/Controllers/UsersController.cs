using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyChat.Models;
using MyChat.Services;
using MyChat.ViewModels;

namespace MyChat.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly MyChatContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly EmailService _email;

    public UsersController(MyChatContext context, UserManager<User> userManager, IWebHostEnvironment env, EmailService email)
    {
        _context = context; _userManager = userManager; _env = env; _email = email;
    }
    
    [HttpGet]
    public async Task<IActionResult> Profile(int? id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var targetUser = id.HasValue
            ? await _userManager.FindByIdAsync(id.Value.ToString())
            : currentUser;

        if (targetUser == null) return NotFound();

        var sentCount = await _context.Messages.CountAsync(m => m.UserId == targetUser.Id);
        ViewBag.SentCount = sentCount;
        return View(targetUser);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");

        return View(new EditProfileVm
        {
            UserName = currentUser.UserName!,
            Email = currentUser.Email!,
            BirthDate = currentUser.BirthDate
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileVm model)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid) return View(model);

        bool passwordChanged = false;
        
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var passRes = await _userManager.ChangePasswordAsync(
                currentUser,
                model.OldPassword!,
                model.NewPassword!
            );

            if (!passRes.Succeeded)
            {
                foreach (var e in passRes.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(model);
            }

            passwordChanged = true;
        }

        currentUser.UserName = model.UserName;
        currentUser.Email = model.Email;
        currentUser.BirthDate = model.BirthDate;

        if (model.Avatar != null && model.Avatar.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "images", "avatars");
            Directory.CreateDirectory(dir);

            var fileName = Guid.NewGuid() + Path.GetExtension(model.Avatar.FileName);
            var path = Path.Combine(dir, fileName);

            using var fs = System.IO.File.Create(path);
            await model.Avatar.CopyToAsync(fs);

            currentUser.AvatarPath = "/images/avatars/" + fileName;
        }

        var updateRes = await _userManager.UpdateAsync(currentUser);
        if (!updateRes.Succeeded)
        {
            foreach (var err in updateRes.Errors)
                ModelState.AddModelError("", err.Description);
            return View(model);
        }
        
        if (passwordChanged)
        {
            var message = $"""
            <h3>Ваш пароль был успешно изменён</h3>
            """;

            await _email.SendAsync(currentUser.Email!, "Пароль изменён", message);
            TempData["Ok"] = "Пароль изменён и письмо отправлено";
        }
        else
        {
            TempData["Ok"] = "Профиль обновлён";
        }

        return RedirectToAction(nameof(Profile));
    }


    [HttpPost]
    public async Task<IActionResult> SendMyData()
    {
        var u = await _userManager.GetUserAsync(User);
        if (u == null) return Unauthorized();

        int msgCount = await _context.Messages.CountAsync(m => m.UserId == u.Id);

        var emailService = HttpContext.RequestServices.GetRequiredService<EmailService>();
        await emailService.SendAsync(
            u.Email!,
            "Ваши данные MyChat",
            $"Профиль: {u.UserName}<br/>" +
            $"Email: {u.Email}<br/>" +
            $"Сообщений отправлено: {msgCount}"
        );

        TempData["Ok"] = "Данные отправлены на почту";
        return RedirectToAction(nameof(Profile));
    }

}
