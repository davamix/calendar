using CalendarApi.Data;
using CalendarApi.Endpoints;
using CalendarApi.Models;
using CalendarApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL persistence. The connection string is injected via configuration — in
// containers the `ConnectionStrings__Calendar` env var (see security.md: secrets come from
// the environment, never source / appsettings). Fail fast with a clear message if absent.
var connectionString = builder.Configuration.GetConnectionString("Calendar")
    ?? throw new InvalidOperationException(
        "Missing connection string 'Calendar'. Set the ConnectionStrings__Calendar "
        + "environment variable (e.g. Host=db;Database=calendar;Username=calendar;Password=…).");

builder.Services.AddDbContext<CalendarDbContext>(options =>
    options.UseNpgsql(connectionString));

// Storage abstraction — one generic EF implementation serves both element kinds.
builder.Services.AddScoped<IElementStore<Project>, EfElementStore<Project>>();
builder.Services.AddScoped<IElementStore<WorkTask>, EfElementStore<WorkTask>>();

// Serialize DateOnly/enums in a JSON-friendly way and emit OpenAPI metadata.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddOpenApi();

// Allow external apps (the spec calls for REST integration) to call the API.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Apply pending migrations on startup (fine for the current single-instance deployment),
// then seed illustrative data into an empty DB — Development only, idempotent.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalendarDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
        await SampleData.SeedAsync(db);
}

app.UseCors();
app.UseDefaultFiles();   // serve wwwroot/index.html at "/"
app.UseStaticFiles(new StaticFileOptions
{
    // POC: always revalidate so edited HTML/CSS/JS are picked up on reload (no stale
    // cache after a class rename). A production build would fingerprint asset URLs and
    // cache them long-term instead.
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
});

// OpenAPI document at /openapi/v1.json for external integrators.
app.MapOpenApi();

// REST API surface — identical CRUD + search for both kinds.
app.MapElementApi<Project>("/api/projects", "Projects",
    req => new Project
    {
        Name = req.Name!.Trim(),
        Description = req.Description,
        StartDate = req.StartDate,
        EndDate = req.EndDate,
        Color = req.Color?.Trim(),
    });

app.MapElementApi<WorkTask>("/api/tasks", "Tasks",
    req => new WorkTask
    {
        Name = req.Name!.Trim(),
        Description = req.Description,
        StartDate = req.StartDate,
        EndDate = req.EndDate,
        Color = req.Color?.Trim(),
    });

// Lightweight health probe for the container.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
