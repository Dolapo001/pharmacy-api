using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using PharmacyAPI.Data;
using PharmacyAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Get and validate connection string
var rawConnString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(rawConnString))
{
    throw new InvalidOperationException(
        "FATAL ERROR: Connection string is missing! " +
        "Set DATABASE_URL environment variable or DefaultConnection in appsettings.json");
}

var connString = rawConnString.Trim();

// 2. Convert Heroku/Render style URL if needed
if (connString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        connString = ConvertDatabaseUrl(connString);
        Console.WriteLine("✅ Converted Heroku-style database URL");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FATAL: Failed to convert database URL: {ex.Message}");
        throw;
    }
}

// 3. Validate connection string format
NpgsqlConnectionStringBuilder csb;
try
{
    csb = new NpgsqlConnectionStringBuilder(connString);
    Console.WriteLine($"Using DB: {csb.Host} (Database: {csb.Database})");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ FATAL: Invalid connection string format: {ex.Message}");
    Console.WriteLine($"Connection string: {MaskPassword(connString)}");
    throw;
}

// 4. Validate JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "FATAL: JWT Key must be at least 32 characters long. " +
        "Set JWT_KEY environment variable or check appsettings.json");
}

Console.WriteLine($"JWT Issuer: {builder.Configuration["Jwt:Issuer"]}");

// 5. Test database connection in development
if (builder.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") == "true")
{
    TestDbConnection(csb);
}

// Add services to container
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
builder.Services.AddDbContext<PharmacyContext>(options => 
    options.UseNpgsql(csb.ConnectionString, o => 
    {
        o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        if (!builder.Environment.IsDevelopment())
            o.CommandTimeout(30);
    })
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

// JWT Service Registration
builder.Services.AddScoped<JwtService>();

// JWT Authentication
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

// Authorization Policies
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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    db = csb.Host,
    database = csb.Database,
    timestamp = DateTime.UtcNow 
}));

app.Run();

// Helper methods
string ConvertDatabaseUrl(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port,
        Database = uri.AbsolutePath.Trim('/'),
        Username = uri.UserInfo.Split(':')[0],
        Password = uri.UserInfo.Split(':')[1],
        SslMode = SslMode.Require,
        TrustServerCertificate = true
    }.ConnectionString;
}

void TestDbConnection(NpgsqlConnectionStringBuilder csBuilder)
{
    try
    {
        using var conn = new NpgsqlConnection(csBuilder.ConnectionString);
        conn.Open();
        Console.WriteLine("✅ Database connection test successful!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FATAL: Database connection test failed: {ex.Message}");
        throw;
    }
}

string MaskPassword(string connectionString)
{
    try
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "*****";
        }
        return builder.ConnectionString;
    }
    catch
    {
        // If parsing fails, return masked version
        var idx = connectionString.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        if (idx > 0)
        {
            var sub = connectionString.Substring(idx + 9);
            var endIdx = sub.IndexOf(';');
            var password = endIdx > 0 ? sub.Substring(0, endIdx) : sub;
            return connectionString.Replace(password, "*****");
        }
        return connectionString;
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