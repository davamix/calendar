using CalendarApi.Data;
using CalendarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Services;

/// <summary>
/// EF Core / PostgreSQL implementation of <see cref="IElementStore{T}"/>. A single generic
/// type serves both element kinds via <c>DbContext.Set&lt;T&gt;()</c>, preserving the
/// zero-duplication design the in-memory store had.
/// </summary>
public sealed class EfElementStore<T>(CalendarDbContext db) : IElementStore<T>
    where T : CalendarItem
{
    private DbSet<T> Set => db.Set<T>();

    public async Task<IReadOnlyList<T>> SearchByNameAsync(string? name)
    {
        IQueryable<T> query = Set;

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim();
            // ILIKE-backed, case-insensitive substring match (translated by Npgsql).
            query = query.Where(i => EF.Functions.ILike(i.Name, $"%{term}%"));
        }

        return await query
            .OrderBy(i => i.StartDate)
            .ThenBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id) =>
        await Set.FirstOrDefaultAsync(i => i.Id == id);

    public async Task<T> AddAsync(T item)
    {
        if (item.Id == Guid.Empty)
            item.Id = Guid.NewGuid();

        Set.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<T?> UpdateAsync(Guid id, ElementRequest request)
    {
        var item = await Set.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return null;

        request.ApplyTo(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await Set.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return false;

        Set.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
}
