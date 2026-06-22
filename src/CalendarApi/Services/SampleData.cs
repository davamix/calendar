using CalendarApi.Models;

namespace CalendarApi.Services;

/// <summary>
/// Seeds the in-memory stores with illustrative data. Everything is anchored to the
/// current month so the default month view always has something to show.
/// </summary>
public static class SampleData
{
    public static void Seed(InMemoryStore<Project> projects, InMemoryStore<WorkTask> tasks)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        DateOnly Day(int day) =>
            new(today.Year, today.Month, Math.Clamp(day, 1, lastOfMonth.Day));

        projects.Add(new Project
        {
            Name = "Website Redesign",
            Description = "Revamp the marketing site with the new brand guidelines.",
            StartDate = firstOfMonth,
            EndDate = Day(18),
            Color = "#4f46e5",
        });
        projects.Add(new Project
        {
            Name = "Mobile App v2",
            Description = "Ship the second major release of the companion mobile app.",
            StartDate = Day(8),
            EndDate = lastOfMonth,
            Color = "#0ea5e9",
        });
        projects.Add(new Project
        {
            Name = "Q3 Planning",
            Description = "Roadmap and OKR planning for the next quarter.",
            StartDate = Day(22),
            EndDate = Day(26),
            Color = "#7c3aed",
        });

        tasks.Add(new WorkTask
        {
            Name = "Design review",
            Description = "Walk through the homepage mockups with the design team.",
            StartDate = Day(3),
            EndDate = Day(3),
            Color = "#0d9488",
        });
        tasks.Add(new WorkTask
        {
            Name = "Sprint planning",
            Description = "Plan the upcoming two-week sprint.",
            StartDate = Day(10),
            EndDate = Day(10),
            Color = "#16a34a",
        });
        tasks.Add(new WorkTask
        {
            Name = "API integration spike",
            Description = "Investigate the third-party payment API integration.",
            StartDate = Day(12),
            EndDate = Day(14),
            Color = "#d97706",
        });
        tasks.Add(new WorkTask
        {
            Name = "Release demo",
            Description = "Demo the new features to stakeholders.",
            StartDate = Day(20),
            EndDate = Day(20),
            Color = "#db2777",
        });
        tasks.Add(new WorkTask
        {
            Name = "Retrospective",
            Description = "Team retrospective for the closing sprint.",
            StartDate = Day(27),
            EndDate = Day(27),
            Color = "#475569",
        });
    }
}
