using System.Text;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Observability;
using HireFlow.Hiring.Application;
using HireFlow.Hiring.Infrastructure;
using HireFlow.Hiring.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add cloud observability (Honeycomb OpenTelemetry)
builder.Services.AddHireFlowObservability(builder.Configuration, "HireFlow.Hiring");

// Add services to the container.
builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);
builder.Services.AddHiringApplication();
builder.Services.AddHiringInfrastructure(builder.Configuration);

// Add CORS
var frontendOrigin = builder.Configuration["Frontend:Origin"] 
    ?? builder.Configuration["Frontend__Origin"] 
    ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("HireFlowCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            var allowedOrigins = new List<string> { frontendOrigin.TrimEnd('/') };
            policy.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.AddControllers();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SigningKey"] ?? "hireflow_super_secret_jwt_key_for_local_development_1234567890!";
var issuer = jwtSettings["Issuer"] ?? "HireFlow.Identity";
var audience = jwtSettings["Audience"] ?? "HireFlow.Api";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HireFlow Hiring API",
        Version = "v1",
        Description = "Microservice managing companies, job postings, applications, interviews, and hiring pipeline analytics."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token: {your_token_here}"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

app.UseCors("HireFlowCorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HireFlow Hiring API v1");
        c.SwaggerEndpoint("http://localhost:5218/swagger/v1/swagger.json", "HireFlow Identity API v1");
        c.SwaggerEndpoint("http://localhost:5289/swagger/v1/swagger.json", "HireFlow Notification API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Seed initial companies if empty
using (var scope = app.Services.CreateScope())
{
    try
    {
        await HiringDbSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not seed hiring database on startup (database might not be updated or running yet).");
    }
}

// Standard health endpoints
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", service = "Hiring", version = "1.0.0" }));
app.MapGet("/health/ready", async (HiringDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "Ready", service = "Hiring", database = "Connected" })
        : Results.Problem("Database unavailable", statusCode: 503);
});
app.MapGet("/version", () => Results.Ok(new { service = "HireFlow.Hiring.Api", version = "1.0.0", environment = app.Environment.EnvironmentName }));
app.MapGet("/", () => Results.Ok(new { service = "HireFlow.Hiring.Api", status = "Healthy" }))
   .WithName("Health");

app.MapControllers();

app.Run();

public partial class Program { }

