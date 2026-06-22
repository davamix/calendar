namespace CalendarApi.Models;

/// <summary>
/// Discriminates the two kinds of element the calendar tracks.
/// </summary>
public enum ElementKind
{
    Project,
    Task
}

/// <summary>
/// Base type shared by every schedulable element (projects and tasks).
/// Both kinds expose the same initial properties for this proof of concept.
/// </summary>
public abstract class CalendarItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Hex colour (e.g. <c>#4f46e5</c>) used to fill the element's shape in the UI.</summary>
    public string? Color { get; set; }

    /// <summary>The concrete kind, surfaced in API responses so clients can tell them apart.</summary>
    public abstract ElementKind Kind { get; }

    /// <summary>True when <paramref name="day"/> falls inside the element's date range (inclusive).</summary>
    public bool OccursOn(DateOnly day) => day >= StartDate && day <= EndDate;
}
