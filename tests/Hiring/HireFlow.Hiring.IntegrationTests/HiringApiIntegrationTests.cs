using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HireFlow.Hiring.IntegrationTests;

public class HiringApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HiringApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("EF_PROVIDER", "InMemory");
        });
    }

    private void EnsureDatabaseCreated()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HiringDbContext>();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200Ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJobs_PublicAccess_Returns200Ok()
    {
        var client = _factory.CreateClient();
        EnsureDatabaseCreated();

        var response = await client.GetAsync("/api/v1/jobs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<JobDto>>();
        paged.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCompanies_PublicAccess_Returns200Ok()
    {
        var client = _factory.CreateClient();
        EnsureDatabaseCreated();

        var response = await client.GetAsync("/api/v1/companies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<CompanyDto>>();
        paged.Should().NotBeNull();
    }
}
