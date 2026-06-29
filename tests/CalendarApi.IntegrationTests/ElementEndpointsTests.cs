using System.Net;
using System.Net.Http.Json;
using CalendarApi.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CalendarApi.IntegrationTests;

/// <summary>
/// CRUD, the owner/assignee access model, and the cross-user forgery proofs (ASVS V8). Each test
/// uses unique element names + queries by the returned id so the shared container stays isolated.
/// </summary>
[Collection("Api")]
public sealed class ElementEndpointsTests(ApiFactory factory)
{
    private record ElementDto(Guid Id, string Name, string OwnerId);

    private static async Task<ElementDto> CreateProjectAsync(HttpClient client, string name)
    {
        var res = await client.PostAsJsonAsync("/api/projects/", new
        {
            name,
            description = (string?)null,
            startDate = "2026-06-01",
            endDate = "2026-06-02",
            color = (string?)null,
        });
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await res.Content.ReadFromJsonAsync<ElementDto>())!;
    }

    [Fact]
    public async Task List_Anonymous_Returns401()
    {
        var client = factory.CreateClient();   // no test-user header
        var res = await client.GetAsync("/api/projects/");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_StampsOwner_AndIsVisibleToOwner()
    {
        var client = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(client, $"Owned-{Guid.NewGuid():N}");

        created.OwnerId.Should().Be("user-a");

        var get = await client.GetFromJsonAsync<ElementDto>($"/api/projects/{created.Id}");
        get!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task List_OnlyReturnsOwnOrAssigned()
    {
        var name = $"AliceOnly-{Guid.NewGuid():N}";
        var alice = factory.CreateClientAs("user-a");
        await CreateProjectAsync(alice, name);

        var bob = factory.CreateClientAs("user-b");
        var bobList = await bob.GetFromJsonAsync<List<ElementDto>>("/api/projects/");
        bobList!.Should().NotContain(p => p.Name == name);
    }

    [Fact]
    public async Task GetById_AsNonAssignee_Returns404()
    {
        var alice = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(alice, $"Hidden-{Guid.NewGuid():N}");

        var bob = factory.CreateClientAs("user-b");
        var res = await bob.GetAsync($"/api/projects/{created.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_AsOwner_Succeeds()
    {
        var client = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(client, $"Edit-{Guid.NewGuid():N}");

        var res = await client.PutAsJsonAsync($"/api/projects/{created.Id}", new
        {
            name = "Renamed",
            description = "now with a description",
            startDate = "2026-06-01",
            endDate = "2026-06-03",
            color = "#123456",
        });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_AsNonVisibleUser_Returns404()
    {
        var alice = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(alice, $"NoTouch-{Guid.NewGuid():N}");

        var bob = factory.CreateClientAs("user-b");
        var res = await bob.PutAsJsonAsync($"/api/projects/{created.Id}", new
        {
            name = "Hacked", description = (string?)null,
            startDate = "2026-06-01", endDate = "2026-06-02", color = (string?)null,
        });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);   // not visible → hidden, not 403
    }

    [Fact]
    public async Task AddAssignee_AsOwner_MakesVisible_ButAssigneeCannotEditOrDelete()
    {
        var alice = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(alice, $"Shared-{Guid.NewGuid():N}");

        // Owner assigns Bob.
        var assign = await alice.PostAsJsonAsync($"/api/projects/{created.Id}/assignees", new { userId = "user-b" });
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var bob = factory.CreateClientAs("user-b");

        // Bob can now see it...
        var get = await bob.GetAsync($"/api/projects/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        // ...but cannot edit or delete it (visible-but-not-owner → 403).
        var edit = await bob.PutAsJsonAsync($"/api/projects/{created.Id}", new
        {
            name = "BobEdit", description = (string?)null,
            startDate = "2026-06-01", endDate = "2026-06-02", color = (string?)null,
        });
        edit.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var del = await bob.DeleteAsync($"/api/projects/{created.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddAssignee_AsNonOwner_Returns403()
    {
        var alice = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(alice, $"NoShare-{Guid.NewGuid():N}");
        await alice.PostAsJsonAsync($"/api/projects/{created.Id}/assignees", new { userId = "user-b" });

        // Bob (an assignee, not the owner) cannot add others.
        var bob = factory.CreateClientAs("user-b");
        var res = await bob.PostAsJsonAsync($"/api/projects/{created.Id}/assignees", new { userId = "user-c" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveAssignee_AsOwner_Succeeds_AndHidesFromUser()
    {
        var alice = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(alice, $"Unassign-{Guid.NewGuid():N}");
        await alice.PostAsJsonAsync($"/api/projects/{created.Id}/assignees", new { userId = "user-b" });

        var remove = await alice.DeleteAsync($"/api/projects/{created.Id}/assignees/user-b");
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var bob = factory.CreateClientAs("user-b");
        var get = await bob.GetAsync($"/api/projects/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveAssignee_OwnerCannotBeRemoved_Returns403()
    {
        var alice = factory.CreateClientAs("user-a");
        var created = await CreateProjectAsync(alice, $"OwnerGuard-{Guid.NewGuid():N}");

        // The owner is always an assignee and may not be removed.
        var res = await alice.DeleteAsync($"/api/projects/{created.Id}/assignees/user-a");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Tasks_AreIsolatedPerUser()
    {
        // Tasks reuse the generic code path; pin isolation on /api/tasks too.
        var name = $"AliceTask-{Guid.NewGuid():N}";
        var alice = factory.CreateClientAs("user-a");
        var res = await alice.PostAsJsonAsync("/api/tasks/", new
        {
            name,
            description = (string?)null,
            startDate = "2026-06-01",
            endDate = "2026-06-01",
            color = (string?)null,
        });
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await res.Content.ReadFromJsonAsync<ElementDto>())!;

        var bob = factory.CreateClientAs("user-b");
        var get = await bob.GetAsync($"/api/tasks/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
