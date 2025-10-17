using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MyChat.Models;

public class User : IdentityUser<int>
{
    [Required(ErrorMessage = "Дата рождения обязательна")]
    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }
    
    public string? AvatarPath { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}