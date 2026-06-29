using CalendarApi.Models;
using CalendarApi.Services;

namespace CalendarApi.Endpoints;

/// <summary>
/// Maps a full CRUD + search REST surface for a given element kind. The same code backs both
/// <c>/api/projects</c> and <c>/api/tasks</c>. The whole group requires authentication; reads are
/// isolated to the current user by the store's query filter, and mutations are owner-gated.
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
        var group = app.MapGroup(prefix).WithTags(tag).RequireAuthorization();

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

        // Create:  POST /api/{kind}  (owner auto-assigned in the store)
        group.MapPost("/", async (IElementStore<T> store, ElementRequest request) =>
        {
            var errors = request.Validate();
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var created = await store.AddAsync(factory(request));
            return Results.Created($"{prefix}/{created.Id}", created);
        })
        .WithSummary($"Create a {tag}");

        // Update:  PUT /api/{kind}/{id}  (owner only)
        group.MapPut("/{id:guid}", async (IElementStore<T> store, Guid id, ElementRequest request) =>
        {
            var errors = request.Validate();
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var (status, updated) = await store.UpdateAsync(id, request);
            return status switch
            {
                WriteStatus.Success => Results.Ok(updated),
                WriteStatus.Forbidden => Results.Forbid(),
                _ => Results.NotFound(),
            };
        })
        .WithSummary($"Update a {tag} (owner only)");

        // Delete:  DELETE /api/{kind}/{id}  (owner only)
        group.MapDelete("/{id:guid}", async (IElementStore<T> store, Guid id) =>
            await store.DeleteAsync(id) switch
            {
                WriteStatus.Success => Results.NoContent(),
                WriteStatus.Forbidden => Results.Forbid(),
                _ => Results.NotFound(),
            })
        .WithSummary($"Delete a {tag} (owner only)");

        // Assignees:  GET /api/{kind}/{id}/assignees  (visible to owner + assignees)
        group.MapGet("/{id:guid}/assignees", async (IElementStore<T> store, Guid id) =>
        {
            var ids = await store.GetAssigneeIdsAsync(id);
            return ids is null ? Results.NotFound() : Results.Ok(ids);
        })
        .WithSummary($"List the assignees of a {tag}");

        // Add assignee:  POST /api/{kind}/{id}/assignees  (owner only)
        group.MapPost("/{id:guid}/assignees", async (IElementStore<T> store, Guid id, AssigneeRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(AssigneeRequest.UserId)] = ["UserId is required."],
                });

            return await store.AddAssigneeAsync(id, request.UserId.Trim()) switch
            {
                WriteStatus.Success => Results.NoContent(),
                WriteStatus.Forbidden => Results.Forbid(),
                _ => Results.NotFound(),
            };
        })
        .WithSummary($"Assign a user to a {tag} (owner only)");

        // Remove assignee:  DELETE /api/{kind}/{id}/assignees/{userId}  (owner only)
        group.MapDelete("/{id:guid}/assignees/{userId}", async (IElementStore<T> store, Guid id, string userId) =>
            await store.RemoveAssigneeAsync(id, userId) switch
            {
                WriteStatus.Success => Results.NoContent(),
                WriteStatus.Forbidden => Results.Forbid(),
                _ => Results.NotFound(),
            })
        .WithSummary($"Remove a user from a {tag} (owner only)");

        return group;
    }
}
