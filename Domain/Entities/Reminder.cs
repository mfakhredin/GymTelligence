namespace Domain.Entities;

public sealed class Reminder : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public TimeOnly ReminderTime { get; set; }
    public string DaysOfWeek { get; set; } = "Mon,Wed,Fri";
    public bool IsActive { get; set; } = true;
    public string Channel { get; set; } = "dashboard";
}
