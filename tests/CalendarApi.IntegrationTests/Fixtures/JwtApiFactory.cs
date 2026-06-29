using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CalendarApi.IntegrationTests.Fakes;
using CalendarApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Xunit;

namespace CalendarApi.IntegrationTests.Fixtures;

/// <summary>
/// Hosts the API with the REAL JwtBearer scheme, but reconfigured to validate against a local test
/// signing key (no network/JWKS). Used to assert audience/issuer validation and cross-audience
/// rejection. Mint tokens with <see cref="MintJwt"/>.
/// </summary>
public sealed class JwtApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://test.logto.local/oidc/";
    public const string Audience = "https://calendar.api";
    private static readonly SymmetricSecurityKey Key =
        new(Encoding.UTF8.GetBytes("calendar-integration-test-signing-key-0123456789"));

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg17")
        .Build();

    async Task IAsyncLifetime.InitializeAsync() => await _db.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Calendar", _db.GetConnectionString());
        builder.UseSetting("Logto:Issuer", Issuer);
        builder.UseSetting("Logto:Audience", Audience);
        builder.UseSetting("Logto:Web:ClientId", "test-client-id");
        builder.UseSetting("Logto:Web:ClientSecret", "test-client-secret");
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Validate against the local test key instead of fetching Logto's JWKS.
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.MetadataAddress = null!;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = Key,
                    NameClaimType = "sub",
                };
            });
            services.AddSingleton<ILogtoManagementClient, FakeLogtoManagementClient>();
        });
    }

    /// <summary>Signs a JWT for the given audience + subject with the test key.</summary>
    public static string MintJwt(string audience, string sub)
    {
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: audience,
            claims: [new Claim("sub", sub)],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

[CollectionDefinition("Jwt")]
public sealed class JwtCollection : ICollectionFixture<JwtApiFactory>;
