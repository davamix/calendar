using System.Net;
using System.Net.Http.Headers;
using CalendarApi.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace CalendarApi.IntegrationTests;

/// <summary>The JWT-bearer resource server: audience/issuer validation (ASVS V9/V10).</summary>
[Collection("Jwt")]
public sealed class JwtAuthTests(JwtApiFactory factory)
{
    private HttpClient ClientWithToken(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task ValidBearer_ForCorrectAudience_Returns200()
    {
        var token = JwtApiFactory.MintJwt(JwtApiFactory.Audience, "jwt-user");
        var res = await ClientWithToken(token).GetAsync("/api/projects/");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Bearer_ForWrongAudience_Returns401()
    {
        var token = JwtApiFactory.MintJwt("https://some-other.api", "jwt-user");
        var res = await ClientWithToken(token).GetAsync("/api/projects/");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NoToken_Returns401()
    {
        var res = await factory.CreateClient().GetAsync("/api/projects/");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
