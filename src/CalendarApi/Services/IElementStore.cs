using CalendarApi.Models;

namespace CalendarApi.Services;

/// <summary>
/// Storage abstraction for a single element kind. Endpoints depend on this rather than a
/// concrete store, so the backing implementation (in-memory, EF Core, …) can be swapped.
/// </summary>
public interface IElementStore<T> where T : CalendarItem
{
    /// <summary>Case-insensitive substring search by name; empty query returns everything.</summary>
    Task<IReadOnlyList<T>> SearchByNameAsync(string? name);

    Task<T?> GetByIdAsync(Guid id);

    /// <summary>Persists a new element, assigning an id when one is not already set.</summary>
    Task<T> AddAsync(T item);

    /// <summary>Applies the request to an existing element. Returns it, or null if not found.</summary>
    Task<T?> UpdateAsync(Guid id, ElementRequest request);

    Task<bool> DeleteAsync(Guid id);
}
