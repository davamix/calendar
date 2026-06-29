using System.Text.Json.Serialization;

namespace CalendarApi.Models;

/// <summary>A work project. Typically spans several days or weeks.</summary>
public sealed class Project : CalendarItem
{
    public override ElementKind Kind => ElementKind.Project;

    /// <summary>
    /// Users (incl. the owner) who can see this project. Not serialized (would create a cycle and
    /// leak the list); assignees are exposed via the dedicated <c>/assignees</c> endpoint.
    /// </summary>
    [JsonIgnore]
    public ICollection<ProjectAssignee> Assignees { get; } = new List<ProjectAssignee>();
}
