using HireFlow.Identity.Infrastructure;
using HireFlow.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Seed initial roles and demo user logins
using (var scope = app.Services.CreateScope())
{
    try
    {
        await IdentityDbSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not seed database on startup (database might not be updated or running yet).");
    }
}

app.MapGet("/", () => Results.Ok(new { Service = "HireFlow.Identity.Api", Status = "Healthy" }))
   .WithName("Health");

app.Run();
