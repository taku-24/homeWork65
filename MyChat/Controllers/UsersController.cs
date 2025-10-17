using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyChat.Models;
using MyChat.ViewModels;

namespace MyChat.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly MyChatContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _env;

    public UsersController(MyChatContext context, UserManager<User> userManager, IWebHostEnvironment env)
    {
        _context = context; _userManager = userManager; _env = env;
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

        currentUser.UserName = model.UserName;
        currentUser.Email = model.Email;
        currentUser.BirthDate = model.BirthDate;

        if (model.Avatar != null && model.Avatar.Length > 0)
        {
            var avatarsDir = Path.Combine(_env.WebRootPath, "images", "avatars");
            Directory.CreateDirectory(avatarsDir);

            var fileName = Guid.NewGuid() + Path.GetExtension(model.Avatar.FileName);
            var physicalPath = Path.Combine(avatarsDir, fileName);

            using var stream = System.IO.File.Create(physicalPath);
            await model.Avatar.CopyToAsync(stream);

            currentUser.AvatarPath = "/images/avatars/" + fileName;
        }

        var updateResult = await _userManager.UpdateAsync(currentUser);
        if (!updateResult.Succeeded)
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError("", error.Description);

        if (!ModelState.IsValid) return View(model);
        return RedirectToAction(nameof(Profile));
    }
}
