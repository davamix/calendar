using CalendarApi.Data;
using CalendarApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalendarApi.Services;

/// <summary>
/// Seeds illustrative data so a fresh (empty) database has something to show. Idempotent: only
/// writes when the tables are empty (checked with the query filter bypassed, since seeding runs
/// with no HTTP user). Everything is owned by a configurable dev user. Development only.
/// </summary>
public static class SampleData
{
    public static async Task SeedAsync(CalendarDbContext db, IConfiguration config)
    {
        // reason: seeding runs with no authenticated user, so the global query filter would fail
        // closed and hide existing rows — bypass it for the idempotency check only.
        if (await db.Projects.IgnoreQueryFilters().AnyAsync()
            || await db.Tasks.IgnoreQueryFilters().AnyAsync())
            return;

        var ownerId = config["SeedData:OwnerSub"] ?? "dev-user";
        db.Users.Add(new AppUser
        {
            Id = ownerId,
            DisplayName = "Dev User",
            Email = "dev@calendar.local",
        });

        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        DateOnly Day(int day) =>
            new(today.Year, today.Month, Math.Clamp(day, 1, lastOfMonth.Day));

        var projects = new[]
        {
            NewProject("Website Redesign", "Revamp the marketing site with the new brand guidelines.", firstOfMonth, Day(18), "#002366"),
            NewProject("Mobile App v2", "Ship the second major release of the companion mobile app.", Day(8), lastOfMonth, "#4b0082"),
            NewProject("Q3 Planning", "Roadmap and OKR planning for the next quarter.", Day(22), Day(26), "#673147"),
        };

        var tasks = new[]
        {
            NewTask("Design review", "Walk through the homepage mockups with the design team.", Day(3), Day(3), "#004d40"),
            NewTask("Sprint planning", "Plan the upcoming two-week sprint.", Day(10), Day(10), "#8a9a5b"),
            NewTask("API integration spike", "Investigate the third-party payment API integration.", Day(12), Day(14), "#c04000"),
            NewTask("Release demo", "Demo the new features to stakeholders.", Day(20), Day(20), "#708090"),
            NewTask("Retrospective", "Team retrospective for the closing sprint.", Day(27), Day(27), "#36454f"),
        };

        db.Projects.AddRange(projects);
        db.Tasks.AddRange(tasks);
        // The owner is auto-assigned (same rule the store applies on create).
        db.ProjectAssignees.AddRange(projects.Select(p => new ProjectAssignee { ProjectId = p.Id, UserId = ownerId }));
        db.TaskAssignees.AddRange(tasks.Select(t => new TaskAssignee { TaskId = t.Id, UserId = ownerId }));

        await db.SaveChangesAsync();

        Project NewProject(string name, string desc, DateOnly start, DateOnly end, string color) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = desc,
            StartDate = start,
            EndDate = end,
            Color = color,
            OwnerId = ownerId,
            CreatedBy = ownerId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        WorkTask NewTask(string name, string desc, DateOnly start, DateOnly end, string color) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = desc,
            StartDate = start,
            EndDate = end,
            Color = color,
            OwnerId = ownerId,
            CreatedBy = ownerId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
