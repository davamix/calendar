using System.Collections.Concurrent;
using CalendarApi.Models;

namespace CalendarApi.Services;

/// <summary>
/// Thread-safe in-memory repository for a single element kind.
/// Registered once per <see cref="CalendarItem"/> subtype as a singleton, so the
/// same CRUD/search logic is shared by projects and tasks without duplication.
/// </summary>
public sealed class InMemoryStore<T> where T : CalendarItem
{
    private readonly ConcurrentDictionary<Guid, T> _items = new();

    /// <summary>All elements, ordered by start date then name.</summary>
    public IReadOnlyList<T> GetAll() =>
        _items.Values
            .OrderBy(i => i.StartDate)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public T? GetById(Guid id) => _items.TryGetValue(id, out var item) ? item : null;

    /// <summary>Case-insensitive substring search by name. Empty query returns everything.</summary>
    public IReadOnlyList<T> SearchByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return GetAll();

        return _items.Values
            .Where(i => i.Name.Contains(name.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.StartDate)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Adds the element, assigning a new id when one is not already set.</summary>
    public T Add(T item)
    {
        if (item.Id == Guid.Empty)
            item.Id = Guid.NewGuid();

        _items[item.Id] = item;
        return item;
    }

    /// <summary>Applies the request to an existing element. Returns the updated item, or null if not found.</summary>
    public T? Update(Guid id, ElementRequest request)
    {
        if (!_items.TryGetValue(id, out var item))
            return null;

        item.Name = request.Name!.Trim();
        item.Description = request.Description;
        item.StartDate = request.StartDate;
        item.EndDate = request.EndDate;
        item.Color = request.Color?.Trim();
        return item;
    }

    public bool Delete(Guid id) => _items.TryRemove(id, out _);
}
