using Microsoft.EntityFrameworkCore;
using WasThere.Api.Data;
using WasThere.Api.Services;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to allow longer request timeouts for AI processing
// Google Gemini API can take 20-60+ seconds to analyze high-resolution images
// Setting 5-minute timeout to provide sufficient buffer for legitimate long operations
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
    
    // Disable MinResponseDataRate to prevent connection abortion during long AI processing
    // By default, Kestrel enforces a minimum data rate (240 bytes/5sec) and will abort
    // connections that don't send response data fast enough (typically causes timeout around
    // 30 seconds when no data is sent). During Gemini API calls, the server waits for AI
    // response without sending any data to the client, triggering this limit.
    // Setting to null disables the rate check entirely for long-running operations.
    serverOptions.Limits.MinResponseDataRate = null;
    
    // Also disable MinRequestBodyDataRate to prevent issues with slow uploads
    // This ensures large flyer images can be uploaded without rate-based timeouts
    serverOptions.Limits.MinRequestBodyDataRate = null;
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HttpClient for potential future long-running operations
// Note: Currently, the Google.GenAI SDK uses its own internal HttpClient,
// so this configuration is not actively used but is available for future extensibility
builder.Services.AddHttpClient("LongRunning", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Add Google Gemini service
builder.Services.AddSingleton<IGoogleGeminiService, GoogleGeminiService>();

// Add DateYearInferenceService
builder.Services.AddSingleton<IDateYearInferenceService, DateYearInferenceService>();

// Add FlyerConversionLogger
builder.Services.AddScoped<IFlyerConversionLogger, FlyerConversionLogger>();

// Add DbContext - Use PostgreSQL if connection string is provided, otherwise use in-memory database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ClubEventContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<ClubEventContext>(options =>
        options.UseInMemoryDatabase("ClubEventsDb"));
}

// Add Authentication
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

if (!string.IsNullOrEmpty(auth0Domain) && !string.IsNullOrEmpty(auth0Audience))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = ClaimTypes.NameIdentifier
            };
        });
}

builder.Services.AddAuthorization();

// Add CORS
var corsOrigins = builder.Configuration["CorsOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 
    ?? new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply database migrations if using PostgreSQL
// For production deployments with multiple replicas, consider using a separate migration job
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ClubEventContext>();
    
    // Only run migrations if using PostgreSQL
    if (!string.IsNullOrEmpty(connectionString))
    {
        try
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Applying database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw; // Fail fast if migrations fail
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files from uploads directory
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
