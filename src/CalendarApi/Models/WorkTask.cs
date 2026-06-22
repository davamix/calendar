namespace CalendarApi.Models;

/// <summary>
/// A daily task. Named <c>WorkTask</c> to avoid clashing with
/// <see cref="System.Threading.Tasks.Task"/>; it is exposed as "task" in the API.
/// </summary>
public sealed class WorkTask : CalendarItem
{
    public override ElementKind Kind => ElementKind.Task;
}
