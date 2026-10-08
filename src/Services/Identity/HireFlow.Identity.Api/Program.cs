using System.Text;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Observability;
using HireFlow.Identity.Application;
using HireFlow.Identity.Infrastructure;
using HireFlow.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add cloud observability (Honeycomb OpenTelemetry)
builder.Services.AddHireFlowObservability(builder.Configuration, "HireFlow.Identity");

// Add application layer & infrastructure layer
builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Add CORS
var frontendOrigin = builder.Configuration["Frontend:Origin"] 
    ?? builder.Configuration["Frontend__Origin"] 
    ?? "http://localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("HireFlowCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            if (builder.Environment.IsDevelopment()) return true;

            try
            {
                var uri = new Uri(origin);
                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase)
                    || (Uri.TryCreate(frontendOrigin, UriKind.Absolute, out var frontendUri) && uri.Host.Equals(frontendUri.Host, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Add controllers
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
        Title = "HireFlow Identity API",
        Version = "v1",
        Description = "Microservice managing authentication, user accounts, roles, OTP verification, and JWT token rotation."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token format: {your_token_here}"
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HireFlow Identity API v1");
        c.SwaggerEndpoint("http://localhost:5104/swagger/v1/swagger.json", "HireFlow Hiring API v1");
        c.SwaggerEndpoint("http://localhost:5289/swagger/v1/swagger.json", "HireFlow Notification API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

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

// Standard health endpoints
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", service = "Identity", version = "1.0.0" }));
app.MapGet("/health/ready", async (IdentityDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "Ready", service = "Identity", database = "Connected" })
        : Results.Problem("Database unavailable", statusCode: 503);
});
app.MapGet("/version", () => Results.Ok(new { service = "HireFlow.Identity.Api", version = "1.0.0", environment = app.Environment.EnvironmentName }));
app.MapGet("/", () => Results.Ok(new { service = "HireFlow.Identity.Api", status = "Healthy" }))
   .WithName("Health");

app.MapControllers();

app.Run();

public partial class Program { }

