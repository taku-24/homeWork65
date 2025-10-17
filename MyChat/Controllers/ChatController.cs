using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyChat.Models;

namespace MyChat.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly MyChatContext _context;
    private readonly UserManager<User> _userManager;

    public ChatController(MyChatContext context, UserManager<User> userManager)
    {
        _context = context; _userManager = userManager;
    }
    
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        ViewBag.MeId = currentUser!.Id;
        var lastMessages = await _context.Messages
            .Include(m => m.User)
            .OrderByDescending(m => m.Id).Take(30)
            .OrderBy(m => m.Id)
            .ToListAsync();
        return View(lastMessages);
    }
    
    [HttpPost]
    public async Task<IActionResult> Send([FromForm] string text)
    {
        var trimmedText = (text ?? "").Trim();
        if (string.IsNullOrEmpty(trimmedText) || trimmedText.Length > 150)
            return BadRequest(new { ok = false, message = "Пустое или слишком длинное сообщение" });

        var currentUser = await _userManager.GetUserAsync(User);
        var message = new Message { Text = trimmedText, UserId = currentUser!.Id, SentAt = DateTime.UtcNow };
        _context.Add(message);
        await _context.SaveChangesAsync();

        return Json(new {
            id = message.Id,
            text = message.Text,
            sentAt = message.SentAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            userId = currentUser.Id,
            userName = currentUser.UserName,
            avatar = currentUser.AvatarPath
        });
    }
    
    [HttpGet]
    public async Task<IActionResult> Latest(int afterId = 0)
    {
        var messages = await _context.Messages
            .Include(m => m.User)
            .Where(m => m.Id > afterId)
            .OrderBy(m => m.Id)
            .Select(m => new {
                id = m.Id,
                text = m.Text,
                sentAt = m.SentAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                userId = m.UserId,
                userName = m.User.UserName,
                avatar = m.User.AvatarPath
            })
            .ToListAsync();

        return Json(messages);
    }
}
