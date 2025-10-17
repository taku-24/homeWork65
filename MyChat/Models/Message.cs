using System.ComponentModel.DataAnnotations;

namespace MyChat.Models;

public class Message
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Сообщение не может быть пустым")]
    [StringLength(150, ErrorMessage = "Сообщение не может превышать 150 символов")]
    public string Text { get; set; } = "";

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}