using CalendarApi.Auth;
using CalendarApi.Data;
using CalendarApi.Endpoints;
using CalendarApi.Models;
using CalendarApi.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL persistence. The connection string is injected via configuration — in
// containers the `ConnectionStrings__Calendar` env var (see security.md: secrets come from
// the environment, never source / appsettings). Fail fast with a clear message if absent.
var connectionString = builder.Configuration.GetConnectionString("Calendar")
    ?? throw new InvalidOperationException(
        "Missing connection string 'Calendar'. Set the ConnectionStrings__Calendar "
        + "environment variable (e.g. Host=db;Database=calendar;Username=calendar_app;Password=…).");

builder.Services.AddDbContext<CalendarDbContext>(options =>
    options.UseNpgsql(connectionString));

// Persist the Data Protection key ring in Postgres so auth/antiforgery cookies survive container
// recreates (otherwise every redeploy invalidates sessions). Pinned app name = stable isolation.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<CalendarDbContext>()
    .SetApplicationName("calendar");

// Storage abstraction — one generic EF implementation serves both element kinds.
builder.Services.AddScoped<IElementStore<Project>, EfElementStore<Project>>();
builder.Services.AddScoped<IElementStore<WorkTask>, EfElementStore<WorkTask>>();

// Logto auth (BFF cookie + JWT bearer), authorization policy, antiforgery, rate limiting,
// ICurrentUser. See Auth/AuthenticationExtensions.cs and docs/auth.md.
builder.AddCalendarAuth();

// User directory (assignee picker) via the Logto Management API.
builder.Services.AddLogtoManagementClient(builder.Configuration);

// Serialize DateOnly/enums in a JSON-friendly way and emit OpenAPI metadata.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending migrations on startup (fine for the current single-instance deployment),
// then seed illustrative data into an empty DB — Development only, idempotent.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
        await SampleData.SeedAsync(db, app.Configuration);
}

// Behind the reverse proxy (Caddy standalone / shared proxy when integrated), honor
// X-Forwarded-Proto/For so the app sees the real scheme (Secure cookies, correct redirects) and
// client IP — but only from private-range peers, so external clients can't spoof them (ASVS V4.1.3).
var forwarded = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwarded.KnownNetworks.Clear();
forwarded.KnownProxies.Clear();
forwarded.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("10.0.0.0"), 8));
forwarded.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("172.16.0.0"), 12));
forwarded.KnownNetworks.Add(new IPNetwork(System.Net.IPAddress.Parse("192.168.0.0"), 16));
app.UseForwardedHeaders(forwarded);

app.UseAuthentication();
app.UseAuthorization();
// After auth so the limiter can partition by the authenticated subject (falls back to IP).
app.UseRateLimiter();

// API responses must not be cached by intermediaries/browser (ASVS V14).
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api"))
        ctx.Response.Headers.CacheControl = "no-store";
    await next();
});

// Don't serve the SPA shell to anonymous users — redirect HTML navigations straight to sign-in
// so the calendar never flashes before the login redirect. API/auth/health paths are exempt; the
// SPA's own 401 handling stays as a fallback for mid-session expiry.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;
    var isHtmlNav = HttpMethods.IsGet(ctx.Request.Method)
        && ctx.Request.Headers.Accept.ToString().Contains("text/html");
    var isAppShell = !path.StartsWithSegments("/api")
        && !path.StartsWithSegments("/login")
        && !path.StartsWithSegments("/logout")
        && !path.StartsWithSegments("/signin-oidc")
        && !path.StartsWithSegments("/signout-callback-oidc")
        && !path.StartsWithSegments("/health");
    if (isHtmlNav && isAppShell && !(ctx.User.Identity?.IsAuthenticated ?? false))
    {
        ctx.Response.Redirect("/login");
        return;
    }
    await next();
});

// CSRF protection for the cookie/BFF path; issues the SPA's token on HTML navigations.
// Runs before static files so the index.html response carries the token cookie.
app.UseSpaAntiforgery();

app.UseDefaultFiles();   // serve wwwroot/index.html at "/"
app.UseStaticFiles(new StaticFileOptions
{
    // Dev: always revalidate so edited HTML/CSS/JS are picked up on reload (no stale
    // cache after a class rename). A production build would fingerprint asset URLs and
    // cache them long-term instead.
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
});

// OpenAPI document at /openapi/v1.json for external integrators.
app.MapOpenApi();

// BFF auth surface: /login, /logout, /api/me.
app.MapAuthEndpoints();

// REST API surface — identical CRUD + search for both kinds. The create factory reuses
// ElementRequest.ApplyTo (same mapping as the update path) so the two can't drift.
app.MapElementApi<Project>("/api/projects", "Projects", req => req.ApplyTo(new Project()));
app.MapElementApi<WorkTask>("/api/tasks", "Tasks", req => req.ApplyTo(new WorkTask()));

// User directory for the assignee picker.
app.MapUserEndpoints();

// Lightweight health probe for the container (anonymous).
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposed for WebApplicationFactory in the integration tests.
public partial class Program;
