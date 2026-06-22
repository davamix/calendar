using CalendarApi.Endpoints;
using CalendarApi.Models;
using CalendarApi.Services;

var builder = WebApplication.CreateBuilder(args);

// In-memory stores (one per element kind), shared for the lifetime of the app.
builder.Services.AddSingleton<InMemoryStore<Project>>();
builder.Services.AddSingleton<InMemoryStore<WorkTask>>();

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

// Seed illustrative sample data into the in-memory stores.
SampleData.Seed(
    app.Services.GetRequiredService<InMemoryStore<Project>>(),
    app.Services.GetRequiredService<InMemoryStore<WorkTask>>());

app.UseCors();
app.UseDefaultFiles();   // serve wwwroot/index.html at "/"
app.UseStaticFiles();

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
