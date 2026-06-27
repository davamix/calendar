using CalendarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Data;

/// <summary>
/// EF Core context backing the calendar. Projects and tasks share the same column shape but
/// live in their own tables (queried independently — no inheritance mapping needed).
/// </summary>
public sealed class CalendarDbContext(DbContextOptions<CalendarDbContext> options)
    : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Configure(modelBuilder.Entity<Project>(), "projects");
        Configure(modelBuilder.Entity<WorkTask>(), "tasks");
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
        // DateOnly maps to Postgres `date` natively via Npgsql.

        // Kind is derived from the concrete type (no setter) — never persist it.
        entity.Ignore(e => e.Kind);
    }
}
