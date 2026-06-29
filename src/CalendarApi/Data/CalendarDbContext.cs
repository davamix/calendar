using CalendarApi.Models;
using CalendarApi.Services;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Data;

/// <summary>
/// EF Core context backing the calendar. Projects and tasks share the same column shape but
/// live in their own tables. A global query filter keyed off <see cref="ICurrentUser"/> enforces
/// per-user read isolation (owner or assignee) — see docs/security/asvs-l2/V08-authorization.md.
/// Also stores the Data Protection key ring so auth/antiforgery cookies survive container recreates.
/// </summary>
public sealed class CalendarDbContext : DbContext, IDataProtectionKeyContext
{
    // Captured once per context instance; EF parameterises it into the compiled query filter.
    private readonly string? _currentUserId;

    public CalendarDbContext(DbContextOptions<CalendarDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        _currentUserId = currentUser.Id;
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ProjectAssignee> ProjectAssignees => Set<ProjectAssignee>();
    public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();

    /// <summary>Data Protection key ring (persisted so cookies survive restarts).</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Configure(modelBuilder.Entity<Project>(), "projects");
        Configure(modelBuilder.Entity<WorkTask>(), "tasks");

        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasMaxLength(64);
            e.Property(u => u.Email).HasMaxLength(320);
            e.Property(u => u.DisplayName).HasMaxLength(200);
        });

        modelBuilder.Entity<ProjectAssignee>(e =>
        {
            e.ToTable("project_assignees");
            e.HasKey(a => new { a.ProjectId, a.UserId });
            e.Property(a => a.UserId).HasMaxLength(64);
            e.HasOne(a => a.Project).WithMany(p => p.Assignees)
                .HasForeignKey(a => a.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.User).WithMany()
                .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskAssignee>(e =>
        {
            e.ToTable("task_assignees");
            e.HasKey(a => new { a.TaskId, a.UserId });
            e.Property(a => a.UserId).HasMaxLength(64);
            e.HasOne(a => a.Task).WithMany(t => t.Assignees)
                .HasForeignKey(a => a.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.User).WithMany()
                .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Per-user read isolation: a user sees only elements they own or are assigned to.
        // An unset current user (no HTTP context) matches no row — fail closed (ASVS V8.4.1).
        modelBuilder.Entity<Project>().HasQueryFilter(p =>
            p.OwnerId == _currentUserId || p.Assignees.Any(a => a.UserId == _currentUserId));
        modelBuilder.Entity<WorkTask>().HasQueryFilter(t =>
            t.OwnerId == _currentUserId || t.Assignees.Any(a => a.UserId == _currentUserId));
    }

    /// <summary>Shared column configuration for both element kinds.</summary>
    private static void Configure<T>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entity,
        string table)
        where T : CalendarItem
    {
        entity.ToTable(table);
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Color).HasMaxLength(7);   // "#rrggbb"
        entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(64);
        entity.Property(e => e.CreatedBy).HasMaxLength(64);
        entity.HasIndex(e => e.OwnerId);
        entity.HasOne<AppUser>().WithMany()
            .HasForeignKey(e => e.OwnerId).OnDelete(DeleteBehavior.Restrict);
        // DateOnly maps to Postgres `date` natively via Npgsql.

        // Kind is derived from the concrete type (no setter) — never persist it.
        entity.Ignore(e => e.Kind);
    }

    // --- Assignee helpers (the one place the per-kind switch lives) -------------------------

    /// <summary>Ensures a local user row exists for the given Logto sub (FK target for owner/assignee).</summary>
    public async Task EnsureUserAsync(string userId)
    {
        if (!await Users.AnyAsync(u => u.Id == userId))
            Users.Add(new AppUser { Id = userId });
    }

    /// <summary>Adds an assignee join row for the element (no save).</summary>
    public void AddAssignee(CalendarItem item, string userId)
    {
        switch (item)
        {
            case Project p: ProjectAssignees.Add(new ProjectAssignee { ProjectId = p.Id, UserId = userId }); break;
            case WorkTask t: TaskAssignees.Add(new TaskAssignee { TaskId = t.Id, UserId = userId }); break;
        }
    }

    public Task<bool> HasAssigneeAsync(CalendarItem item, string userId) => item switch
    {
        Project p => ProjectAssignees.AnyAsync(a => a.ProjectId == p.Id && a.UserId == userId),
        WorkTask t => TaskAssignees.AnyAsync(a => a.TaskId == t.Id && a.UserId == userId),
        _ => Task.FromResult(false),
    };

    public async Task RemoveAssigneeAsync(CalendarItem item, string userId)
    {
        switch (item)
        {
            case Project p:
                await ProjectAssignees.Where(a => a.ProjectId == p.Id && a.UserId == userId).ExecuteDeleteAsync();
                break;
            case WorkTask t:
                await TaskAssignees.Where(a => a.TaskId == t.Id && a.UserId == userId).ExecuteDeleteAsync();
                break;
        }
    }

    public async Task<IReadOnlyList<string>> AssigneeIdsAsync(CalendarItem item) => item switch
    {
        Project p => await ProjectAssignees.Where(a => a.ProjectId == p.Id).Select(a => a.UserId).ToListAsync(),
        WorkTask t => await TaskAssignees.Where(a => a.TaskId == t.Id).Select(a => a.UserId).ToListAsync(),
        _ => [],
    };
}
