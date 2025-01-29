using System.ComponentModel.DataAnnotations;

namespace Bot.Models;

[Flags]
public enum Include
{
    Default = 0,
    [Display(Name = "Имя пользователя")]
    User = 1 << 0,
    [Display(Name = "Комментарий")]
    Comment = 1 << 1,
    [Display(Name = "Время")]
    Time = 1 << 2
}