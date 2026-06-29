using System.Text.Json.Serialization;

namespace CalendarApi.Models;

/// <summary>
/// A daily task. Named <c>WorkTask</c> to avoid clashing with
/// <see cref="System.Threading.Tasks.Task"/>; it is exposed as "task" in the API.
/// </summary>
public sealed class WorkTask : CalendarItem
{
    public override ElementKind Kind => ElementKind.Task;

    /// <summary>
    /// Users (incl. the owner) who can see this task. Not serialized (would create a cycle and leak
    /// the list); assignees are exposed via the dedicated <c>/assignees</c> endpoint.
    /// </summary>
    [JsonIgnore]
    public ICollection<TaskAssignee> Assignees { get; } = new List<TaskAssignee>();
}
