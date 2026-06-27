using CalendarApi.Models;
using CalendarApi.Services;

namespace CalendarApi.Endpoints;

/// <summary>
/// Maps a full CRUD + search REST surface for a given element kind. The same code
/// backs both <c>/api/projects</c> and <c>/api/tasks</c>.
/// </summary>
public static class ElementEndpoints
{
    /// <param name="app">Endpoint route builder.</param>
    /// <param name="prefix">Route prefix, e.g. <c>/api/projects</c>.</param>
    /// <param name="tag">OpenAPI tag / group name.</param>
    /// <param name="factory">Creates a new element instance from a validated request.</param>
    public static RouteGroupBuilder MapElementApi<T>(
        this IEndpointRouteBuilder app,
        string prefix,
        string tag,
        Func<ElementRequest, T> factory)
        where T : CalendarItem
    {
        var group = app.MapGroup(prefix).WithTags(tag);

        // List + search by name:  GET /api/{kind}?name=foo
        group.MapGet("/", async (IElementStore<T> store, string? name) =>
            Results.Ok(await store.SearchByNameAsync(name)))
            .WithSummary($"List or search {tag} by name");

        // Get by id:  GET /api/{kind}/{id}
        group.MapGet("/{id:guid}", async (IElementStore<T> store, Guid id) =>
        {
            var item = await store.GetByIdAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithSummary($"Get a single {tag} by id");

        // Create:  POST /api/{kind}
        group.MapPost("/", async (IElementStore<T> store, ElementRequest request) =>
        {
            var errors = request.Validate();
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var created = await store.AddAsync(factory(request));
            return Results.Created($"{prefix}/{created.Id}", created);
        })
        .WithSummary($"Create a {tag}");

        // Update:  PUT /api/{kind}/{id}
        group.MapPut("/{id:guid}", async (IElementStore<T> store, Guid id, ElementRequest request) =>
        {
            var errors = request.Validate();
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var updated = await store.UpdateAsync(id, request);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        })
        .WithSummary($"Update a {tag}");

        // Delete:  DELETE /api/{kind}/{id}
        group.MapDelete("/{id:guid}", async (IElementStore<T> store, Guid id) =>
            await store.DeleteAsync(id) ? Results.NoContent() : Results.NotFound())
        .WithSummary($"Delete a {tag}");

        return group;
    }
}
