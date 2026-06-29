using CalendarApi.Data;
using CalendarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Services;

/// <summary>
/// EF Core / PostgreSQL implementation of <see cref="IElementStore{T}"/>. Reads flow through the
/// DbContext global query filter (owner-or-assignee isolation); writes are owner-gated.
/// </summary>
public sealed class EfElementStore<T>(CalendarDbContext db, ICurrentUser currentUser) : IElementStore<T>
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
        var uid = RequireUser();
        if (item.Id == Guid.Empty)
            item.Id = Guid.NewGuid();

        item.OwnerId = uid;
        item.CreatedBy = uid;
        item.CreatedAt = DateTimeOffset.UtcNow;

        await db.EnsureUserAsync(uid);
        Set.Add(item);
        db.AddAssignee(item, uid);   // creator is auto-assigned (ASVS V8: "visible = owner or assignee")
        await db.SaveChangesAsync();
        return item;
    }

    public async Task<(WriteStatus Status, T? Item)> UpdateAsync(Guid id, ElementRequest request)
    {
        var item = await Set.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return (WriteStatus.NotFound, null);
        if (item.OwnerId != currentUser.Id)
            return (WriteStatus.Forbidden, null);

        request.ApplyTo(item);
        await db.SaveChangesAsync();
        return (WriteStatus.Success, item);
    }

    public async Task<WriteStatus> DeleteAsync(Guid id)
    {
        var item = await Set.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return WriteStatus.NotFound;
        if (item.OwnerId != currentUser.Id)
            return WriteStatus.Forbidden;

        Set.Remove(item);
        await db.SaveChangesAsync();
        return WriteStatus.Success;
    }

    public async Task<IReadOnlyList<string>?> GetAssigneeIdsAsync(Guid id)
    {
        var item = await Set.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return null;
        return await db.AssigneeIdsAsync(item);
    }

    public async Task<WriteStatus> AddAssigneeAsync(Guid id, string userId)
    {
        var item = await Set.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return WriteStatus.NotFound;
        if (item.OwnerId != currentUser.Id)
            return WriteStatus.Forbidden;

        if (!await db.HasAssigneeAsync(item, userId))
        {
            await db.EnsureUserAsync(userId);
            db.AddAssignee(item, userId);
            await db.SaveChangesAsync();
        }
        return WriteStatus.Success;
    }

    public async Task<WriteStatus> RemoveAssigneeAsync(Guid id, string userId)
    {
        var item = await Set.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null)
            return WriteStatus.NotFound;
        if (item.OwnerId != currentUser.Id)
            return WriteStatus.Forbidden;
        if (userId == item.OwnerId)
            return WriteStatus.Forbidden;   // the owner is always an assignee and can't be removed

        await db.RemoveAssigneeAsync(item, userId);
        return WriteStatus.Success;
    }

    private string RequireUser() =>
        currentUser.Id ?? throw new InvalidOperationException("No authenticated user on the request.");
}
