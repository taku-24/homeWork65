using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyChat.Models;
using MyChat.ViewModels;

namespace MyChat.Controllers;

[Authorize(Roles = "admin")]
public class AdminController : Controller
{
    private readonly UserManager<User> _userManager;

    public AdminController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }
    
    public IActionResult Index()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }
    
    [HttpPost]
    public async Task<IActionResult> Lock(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(1));

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, locked = true });

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
            await _userManager.SetLockoutEndDateAsync(user, null);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, locked = false });

        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var viewModel = new EditProfileVm
        {
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            BirthDate = user.BirthDate
        };

        return View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditProfileVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.BirthDate = model.BirthDate;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }
    
    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterVm model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var age = (int)((DateTime.UtcNow - model.BirthDate).TotalDays / 365.25);
        if (age < 18)
        {
            ModelState.AddModelError(nameof(model.BirthDate), "Регистрация доступна только с 18 лет.");
            return View(model);
        }

        var newUser = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            BirthDate = model.BirthDate,
            AvatarPath = "/images/avatars/default.png"
        };

        var createResult = await _userManager.CreateAsync(newUser, model.Password);
        if (createResult.Succeeded)
        {
            await _userManager.AddToRoleAsync(newUser, "user");
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in createResult.Errors)
            ModelState.AddModelError("", error.Description);

        return View(model);
    }
}
