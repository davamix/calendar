using CalendarApi.Data;
using CalendarApi.Models;

namespace CalendarApi.Services;

/// <summary>
/// Seeds illustrative data so a fresh (empty) database has something to show in the default
/// month view. Idempotent: only writes when the tables are empty, so it never clobbers real
/// data on restart. Intended for Development only (see <c>Program.cs</c>).
/// </summary>
public static class SampleData
{
    public static async Task SeedAsync(CalendarDbContext db)
    {
        // Already populated — leave existing data untouched.
        if (db.Projects.Any() || db.Tasks.Any())
            return;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        DateOnly Day(int day) =>
            new(today.Year, today.Month, Math.Clamp(day, 1, lastOfMonth.Day));

        db.Projects.AddRange(
            new Project
            {
                Name = "Website Redesign",
                Description = "Revamp the marketing site with the new brand guidelines.",
                StartDate = firstOfMonth,
                EndDate = Day(18),
                Color = "#002366", // Deep Royal Blue
            },
            new Project
            {
                Name = "Mobile App v2",
                Description = "Ship the second major release of the companion mobile app.",
                StartDate = Day(8),
                EndDate = lastOfMonth,
                Color = "#4b0082", // Muted Indigo
            },
            new Project
            {
                Name = "Q3 Planning",
                Description = "Roadmap and OKR planning for the next quarter.",
                StartDate = Day(22),
                EndDate = Day(26),
                Color = "#673147", // Deep Plum
            });

        db.Tasks.AddRange(
            new WorkTask
            {
                Name = "Design review",
                Description = "Walk through the homepage mockups with the design team.",
                StartDate = Day(3),
                EndDate = Day(3),
                Color = "#004d40", // Forest Teal
            },
            new WorkTask
            {
                Name = "Sprint planning",
                Description = "Plan the upcoming two-week sprint.",
                StartDate = Day(10),
                EndDate = Day(10),
                Color = "#8a9a5b", // Soft Sage Green
            },
            new WorkTask
            {
                Name = "API integration spike",
                Description = "Investigate the third-party payment API integration.",
                StartDate = Day(12),
                EndDate = Day(14),
                Color = "#c04000", // Warm Terracotta
            },
            new WorkTask
            {
                Name = "Release demo",
                Description = "Demo the new features to stakeholders.",
                StartDate = Day(20),
                EndDate = Day(20),
                Color = "#708090", // Cool Slate Gray
            },
            new WorkTask
            {
                Name = "Retrospective",
                Description = "Team retrospective for the closing sprint.",
                StartDate = Day(27),
                EndDate = Day(27),
                Color = "#36454f", // Charcoal
            });

        await db.SaveChangesAsync();
    }
}
