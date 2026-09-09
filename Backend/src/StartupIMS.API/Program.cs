using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StartupIMS.API.Middleware;
using StartupIMS.Infrastructure.Persistence;
using StartupIMS.Infrastructure.Services;
using StartupIMS.Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

// --- Logging ---
// Serilog replaces the default .NET logger entirely. Structured logging (not
// just plain text) means log fields like {Method} and {Path} in
// GlobalExceptionHandler stay queryable, not just concatenated into a string -
// matters once logs go anywhere searchable (Seq, ELK, Application Insights).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/startupims-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14));

// --- Exception handling ---
// Registering this explicitly means ASP.NET Core will NOT auto-add the
// Developer Exception Page middleware, even in Development - all unhandled
// exceptions now go through GlobalExceptionHandler instead, giving one
// consistent JSON error shape (and no leaked stack traces once deployed).
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- Configuration ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

// --- DbContexts (one per module, per our schema-per-module rule) ---
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("IdentityDb"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("IdentityDb"))));

builder.Services.AddDbContext<CoreDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("CoreDb"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("CoreDb"))));

// --- Services ---
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IStartupVisibilityService, StartupVisibilityService>();
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection(FileStorageSettings.SectionName));
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.Configure<PaymentServiceSettings>(builder.Configuration.GetSection(PaymentServiceSettings.SectionName));
builder.Services.AddHttpClient<IPaymentServiceClient, HttpPaymentServiceClient>();

// --- Auth: JWT bearer + role-based policies for the 3 platform roles ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MentorOnly", policy => policy.RequireRole("Mentor"));
    options.AddPolicy("FounderOnly", policy => policy.RequireRole("Founder"));
    options.AddPolicy("AdminOrMentor", policy => policy.RequireRole("Admin", "Mentor"));
});

// --- MVC / Swagger ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// Logs one line per request (method, path, status, duration) - separate from
// GlobalExceptionHandler, which only logs when something actually breaks.
app.UseSerilogRequestLogging();

// First in the pipeline - catches exceptions from everything after it
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
