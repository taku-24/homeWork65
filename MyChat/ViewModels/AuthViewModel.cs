using System.ComponentModel.DataAnnotations;

namespace MyChat.ViewModels;

public class RegisterVm
{
    [Required(ErrorMessage = "Логин обязателен")]
    [Display(Name = "Имя пользователя")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Дата рождения обязательна")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата рождения")]
    public DateTime BirthDate { get; set; }

    [Display(Name = "Аватар")]
    public IFormFile? Avatar { get; set; }

    [Required, DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginVm
{
    [Required, Display(Name = "Имя пользователя или Email")]
    public string UserNameOrEmail { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Пароль")]
    public string Password { get; set; } = "";

    [Display(Name = "Запомнить меня")] public bool RememberMe { get; set; }
}


public class EditProfileVm : IValidatableObject
{
    [Required]
    public string UserName { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    public IFormFile? Avatar { get; set; }
    
    [DataType(DataType.Password)]
    public string? OldPassword { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    public string? ConfirmPassword { get; set; }

    
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!string.IsNullOrWhiteSpace(NewPassword))
        {
            if (string.IsNullOrWhiteSpace(OldPassword))
                yield return new ValidationResult("Укажите текущий пароль", new[] { nameof(OldPassword) });

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
                yield return new ValidationResult("Подтвердите новый пароль", new[] { nameof(ConfirmPassword) });
        }
    }
}
