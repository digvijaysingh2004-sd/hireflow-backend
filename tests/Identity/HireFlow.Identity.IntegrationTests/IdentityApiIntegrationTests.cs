using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HireFlow.Identity.Application.DTOs;
using HireFlow.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HireFlow.Identity.IntegrationTests;

public class IdentityApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IdentityApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "EF_PROVIDER", "InMemory" }
                });
            });
        });
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        context.Database.EnsureCreated();
        await IdentityDbSeeder.SeedAsync(scope.ServiceProvider);
    }

    [Fact]
    public async Task HealthEndpoint_Returns200Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_ValidRequest_CreatesUserAccount()
    {
        var client = _factory.CreateClient();
        await SeedDatabaseAsync();

        var request = new RegisterRequest($"testuser_{Guid.NewGuid():N}@example.com", "SecurePass123!", "Test", "User", "Candidate");

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_SeededCandidate_Returns200WithTokens()
    {
        var client = _factory.CreateClient();
        await SeedDatabaseAsync();

        var loginRequest = new LoginRequest("candidate@hireflow.local", "Candidate123!");
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
    }
}
