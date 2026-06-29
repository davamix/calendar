using CalendarApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CalendarApi.Data;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the model (and scaffold migrations) without
/// running the app — avoiding the runtime connection-string requirement and HTTP-bound
/// <see cref="ICurrentUser"/>. Not used at runtime.
/// </summary>
public sealed class CalendarDbContextFactory : IDesignTimeDbContextFactory<CalendarDbContext>
{
    public CalendarDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CalendarDbContext>()
            .UseNpgsql("Host=localhost;Database=calendar;Username=calendar_app;Password=design-time")
            .Options;
        return new CalendarDbContext(options, new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => false;
        public string? Id => null;
        public string? Email => null;
        public string? DisplayName => null;
    }
}
