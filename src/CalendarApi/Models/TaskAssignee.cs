namespace CalendarApi.Models;

/// <summary>Join row: a user assigned to a task (composite key Task+User).</summary>
public sealed class TaskAssignee
{
    public Guid TaskId { get; set; }
    public WorkTask Task { get; set; } = null!;

    /// <summary>The assignee's Logto <c>sub</c>.</summary>
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
}
