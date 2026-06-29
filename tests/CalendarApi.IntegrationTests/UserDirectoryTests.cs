using System.Net;
using System.Net.Http.Json;
using CalendarApi.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CalendarApi.IntegrationTests;

[Collection("Api")]
public sealed class UserDirectoryTests(ApiFactory factory)
{
    private record DirectoryUserDto(string Id, string? Name, string? Email);

    [Fact]
    public async Task Users_Anonymous_Returns401()
    {
        var res = await factory.CreateClient().GetAsync("/api/users");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_Authenticated_ReturnsDirectory()
    {
        var client = factory.CreateClientAs("user-a");
        var users = await client.GetFromJsonAsync<List<DirectoryUserDto>>("/api/users");
        users!.Select(u => u.Id).Should().Contain(["user-a", "user-b", "user-c"]);
    }

    [Fact]
    public async Task Users_Search_Filters()
    {
        var client = factory.CreateClientAs("user-a");
        var users = await client.GetFromJsonAsync<List<DirectoryUserDto>>("/api/users?search=User B");
        users!.Should().ContainSingle().Which.Id.Should().Be("user-b");
    }
}
