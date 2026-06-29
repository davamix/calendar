using CalendarApi.Models;

namespace CalendarApi.Services;

/// <summary>
/// Storage abstraction for a single element kind. Reads are isolated to the current user (owner
/// or assignee) by the DbContext global query filter; writes are owner-gated and return a
/// <see cref="WriteStatus"/> so endpoints can map 404 vs 403. Owner/creator are stamped from the
/// authenticated user, never from request payloads.
/// </summary>
public interface IElementStore<T> where T : CalendarItem
{
    /// <summary>Case-insensitive substring search by name; empty query returns everything visible.</summary>
    Task<IReadOnlyList<T>> SearchByNameAsync(string? name);

    Task<T?> GetByIdAsync(Guid id);

    /// <summary>Persists a new element, stamping owner/creator and auto-assigning the creator.</summary>
    Task<T> AddAsync(T item);

    /// <summary>Owner-only update. Returns the outcome and the updated item on success.</summary>
    Task<(WriteStatus Status, T? Item)> UpdateAsync(Guid id, ElementRequest request);

    /// <summary>Owner-only delete.</summary>
    Task<WriteStatus> DeleteAsync(Guid id);

    /// <summary>Assignee user ids for a visible element, or null if it isn't visible to the caller.</summary>
    Task<IReadOnlyList<string>?> GetAssigneeIdsAsync(Guid id);

    /// <summary>Owner-only: add an assignee.</summary>
    Task<WriteStatus> AddAssigneeAsync(Guid id, string userId);

    /// <summary>Owner-only: remove an assignee (the owner cannot be removed).</summary>
    Task<WriteStatus> RemoveAssigneeAsync(Guid id, string userId);
}
