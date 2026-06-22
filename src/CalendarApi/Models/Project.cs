namespace CalendarApi.Models;

/// <summary>A work project. Typically spans several days or weeks.</summary>
public sealed class Project : CalendarItem
{
    public override ElementKind Kind => ElementKind.Project;
}
