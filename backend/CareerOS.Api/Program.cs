using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Infrastructure.OpenBao;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenBao Configuration Provider if enabled
var openBaoEnabledVal = builder.Configuration["OpenBao:Enabled"]
    ?? Environment.GetEnvironmentVariable("OpenBao__Enabled")
    ?? Environment.GetEnvironmentVariable("BAO_ENABLED");

if (bool.TryParse(openBaoEnabledVal, out var openBaoEnabled) && openBaoEnabled)
{
    var openBaoOptions = new OpenBaoOptions
    {
        Enabled = true,
        Address = builder.Configuration["OpenBao:Address"]
            ?? Environment.GetEnvironmentVariable("BAO_ADDR")
            ?? Environment.GetEnvironmentVariable("VAULT_ADDR")
            ?? "http://localhost:8200",
        RoleId = builder.Configuration["OpenBao:RoleId"]
            ?? Environment.GetEnvironmentVariable("BAO_ROLE_ID")
            ?? Environment.GetEnvironmentVariable("VAULT_ROLE_ID"),
        SecretId = builder.Configuration["OpenBao:SecretId"]
            ?? Environment.GetEnvironmentVariable("BAO_SECRET_ID")
            ?? Environment.GetEnvironmentVariable("VAULT_SECRET_ID"),
        MountPoint = builder.Configuration["OpenBao:MountPoint"]
            ?? Environment.GetEnvironmentVariable("BAO_MOUNT_POINT")
            ?? "secret"
    };

    builder.Configuration.AddOpenBao(openBaoOptions);
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

// Configure JwtOptions
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || Encoding.UTF8.GetBytes(jwtOptions.SecretKey).Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey must be configured and at least 256 bits (32 bytes) long.");
}

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ILinkedinParserService, LinkedinParserService>();
builder.Services.AddScoped<ILinkedinGapAnalysisService, LinkedinGapAnalysisService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddHttpClient<GoogleLoginExchangeService>();
builder.Services.AddHttpContextAccessor();

// Configure DbContext
builder.Services.AddDbContext<CareerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CareerDatabase")));

// Configure Identity Core
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<CareerDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

// Configure Rate Limiting (brute-force protection)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
    {
        var key = context.User.Identity?.Name ??
                  context.Connection.RemoteIpAddress?.ToString() ??
                  "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

// Configure Authentication & JwtBearer
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
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(5),
        NameClaimType = ClaimTypes.Email
    };
});

// Configure Cors
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

// Configure Swagger with Bearer authorization
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
