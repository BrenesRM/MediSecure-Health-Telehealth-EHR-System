using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MediSecureApi.Data;
using MediSecureApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services to the DI Container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration with JWT Bearer authorization support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MediSecure Telehealth & EHR API",
        Version = "v1",
        Description = "Target vulnerable web API designed for Threat Modeling and Vulnerability Analysis (CS 340 Deliverable 1 - ULACIT)."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// Database setup (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=medisecure.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Dependency Injection
builder.Services.AddScoped<IJwtService, JwtService>();

// CORS configuration (allow all origins for lab ease)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 2. Configure HTTP Request Pipeline & Global Exception Handling
// TH-06: Verbose Error Leaks in Debug Mode
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;

        var leakErrors = app.Configuration.GetValue<bool>("SecuritySettings:EnableVerboseErrorStackTraces", true);
        if (leakErrors)
        {
            var errorResponse = new
            {
                error = "InternalServerError",
                message = ex.Message,
                stackTrace = ex.StackTrace, // VULNERABILITY (TH-06): Leaks internal code paths and stack traces
                source = ex.Source,
                timestamp = DateTime.UtcNow
            };
            await context.Response.WriteAsJsonAsync(errorResponse);
        }
        else
        {
            await context.Response.WriteAsJsonAsync(new { error = "An internal error occurred." });
        }
    }
});

app.UseCors("AllowAll");

// Serve Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MediSecure Telehealth API v1");
    c.RoutePrefix = "swagger";
});

// Serve Static Files (Interactive Web Frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

// Ensure Database and Seed Data are created on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(context);
}

app.Run();
