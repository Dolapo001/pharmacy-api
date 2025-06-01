using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using PharmacyAPI.Data;
using PharmacyAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Get connection string - try DATABASE_URL first (common in production), then fallback to DefaultConnection
var connString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connString))
{
    throw new InvalidOperationException("Connection string is missing! Set DATABASE_URL environment variable or DefaultConnection in appsettings.json");
}

// Convert Heroku/Render style DATABASE_URL to connection string format if needed
if (connString.StartsWith("postgres://"))
{
    connString = ConvertDatabaseUrl(connString);
}

var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Key must be at least 32 characters long. Set JWT_KEY environment variable or check appsettings.json");
}

Console.WriteLine($"Using DB: {GetDbHostFromConnectionString(connString)}");
Console.WriteLine($"JWT Issuer: {builder.Configuration["Jwt:Issuer"]}");

// Only test DB connection in development or if explicitly requested
if (builder.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") == "true")
{
    TestDbConnection(connString);
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pharmacy API",
        Version = "v1",
        Description = "API for managing pharmacy shop system"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database Configuration
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<PharmacyContext>(options => 
        options.UseNpgsql(connString, o => 
        {
            o.EnableRetryOnFailure();
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging());
}
else
{
    builder.Services.AddDbContext<PharmacyContext>(options => 
        options.UseNpgsql(connString, o => 
        {
            o.EnableRetryOnFailure();
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            o.CommandTimeout(30); // Add command timeout for production
        }));
}

// JWT Service Registration
builder.Services.AddScoped<JwtService>();

// JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Admin", "Staff"));
});

// CORS Configuration
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(';') 
    ?? Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(';')
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add logging
builder.Services.AddLogging(logging => 
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Apply database migrations
await ApplyMigrationsAsync(app.Services);

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pharmacy API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Helper methods
string ConvertDatabaseUrl(string databaseUrl)
{
    // Convert postgres://user:password@host:port/database to connection string format
    var uri = new Uri(databaseUrl);
    var db = uri.AbsolutePath.Trim('/');
    var userInfo = uri.UserInfo.Split(':');
    
    return $"Host={uri.Host};Port={uri.Port};Database={db};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

string GetDbHostFromConnectionString(string connectionString)
{
    try
    {
        var parts = connectionString.Split(';');
        var hostPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase));
        return hostPart?.Split('=')[1] ?? "Unknown";
    }
    catch
    {
        return "Unknown";
    }
}

void TestDbConnection(string connectionString)
{
    try
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        Console.WriteLine("✅ Database connection successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Connection failed: {ex.Message}");
        throw;
    }
}

async Task ApplyMigrationsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PharmacyContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Checking database connection...");
        
        // Test connection first
        await db.Database.CanConnectAsync();
        logger.LogInformation("Database connection successful!");
        
        logger.LogInformation("Applying migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("✅ Database migrations applied successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Migration failed: {Message}", ex.Message);
        
        if (env.IsDevelopment())
        {
            logger.LogWarning("Attempting to reset database in development...");
            try
            {
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();
                logger.LogInformation("✅ Database reset and migrated successfully!");
            }
            catch (Exception resetEx)
            {
                logger.LogError(resetEx, "❌ Database reset failed: {Message}", resetEx.Message);
                throw;
            }
        }
        else
        {
            logger.LogError("❌ Cannot reset database in production environment");
            throw;
        }
    }
}