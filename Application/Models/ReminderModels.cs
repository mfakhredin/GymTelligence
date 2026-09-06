using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public sealed class SaveReminderRequest
{
    [Required, StringLength(160, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    public TimeOnly ReminderTime { get; set; } = new(18, 0);
    [Required, StringLength(40)] public string DaysOfWeek { get; set; } = "Mon,Wed,Fri";
    public bool IsActive { get; set; } = true;
}

public sealed record ReminderResponse(int Id, string Title, TimeOnly ReminderTime, string DaysOfWeek, bool IsActive);
