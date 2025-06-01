using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using PharmacyAPI.Data;
using PharmacyAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Validate configuration
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing!");
}

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Key must be at least 32 characters long. Check appsettings.json");
}

Console.WriteLine($"Using DB: {connString.Split(';')[0]}");
Console.WriteLine($"JWT Issuer: {builder.Configuration["Jwt:Issuer"]}");

// Test DB connection
TestDbConnection(connString);

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
ApplyMigrations(app.Services);

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
app.Run();

// Helper methods
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

void ApplyMigrations(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PharmacyContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    
    try
    {
        Console.WriteLine("Applying migrations...");
        db.Database.Migrate();
        Console.WriteLine("✅ Database migrations applied!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration failed: {ex.Message}");
        
        if (env.IsDevelopment())
        {
            Console.WriteLine("Resetting database in development...");
            try
            {
                db.Database.EnsureDeleted();
                db.Database.Migrate();
                Console.WriteLine("✅ Database reset and migrated!");
            }
            catch (Exception resetEx)
            {
                Console.WriteLine($"❌ Database reset failed: {resetEx.Message}");
                throw;
            }
        }
        else
        {
            Console.WriteLine("❌ Cannot reset database in production");
            throw;
        }
    }
}