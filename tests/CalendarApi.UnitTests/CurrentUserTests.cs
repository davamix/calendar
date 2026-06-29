using System.Security.Claims;
using CalendarApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace CalendarApi.UnitTests;

public sealed class CurrentUserTests
{
    private static CurrentUser ForPrincipal(ClaimsPrincipal? principal)
    {
        var ctx = new DefaultHttpContext();
        if (principal is not null) ctx.User = principal;
        return new CurrentUser(new HttpContextAccessor { HttpContext = ctx });
    }

    [Fact]
    public void ResolvesSubEmailName_FromClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "abc"),
            new Claim("email", "a@b.c"),
            new Claim("name", "Alice"),
        ], authenticationType: "test"));

        var user = ForPrincipal(principal);

        user.IsAuthenticated.Should().BeTrue();
        user.Id.Should().Be("abc");
        user.Email.Should().Be("a@b.c");
        user.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public void DisplayName_FallsBackToEmail_WhenNoName()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "abc"), new Claim("email", "a@b.c")], "test"));

        ForPrincipal(principal).DisplayName.Should().Be("a@b.c");
    }

    [Fact]
    public void NoHttpContext_IsNotAuthenticated()
    {
        var user = new CurrentUser(new HttpContextAccessor());   // HttpContext is null

        user.IsAuthenticated.Should().BeFalse();
        user.Id.Should().BeNull();
    }
}
