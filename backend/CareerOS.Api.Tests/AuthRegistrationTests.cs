using System.Security.Claims;
using CareerOS.Api.Contracts;
using CareerOS.Api.Controllers;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace CareerOS.Api.Tests;

public class AuthRegistrationTests
{
    private static (AuthController controller, CareerDbContext db) CreateController()
    {
        var dbName = Guid.NewGuid().ToString();
        var dbOptions = new DbContextOptionsBuilder<CareerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new CareerDbContext(dbOptions);

        var identityOptions = Options.Create(new IdentityOptions());

        var userStore = new UserStore<ApplicationUser, IdentityRole<Guid>, CareerDbContext, Guid>(db);
        var userManager = new UserManager<ApplicationUser>(
            userStore,
            identityOptions,
            new PasswordHasher<ApplicationUser>(),
            null!,
            null!,
            null!,
            null!,
            null!,
            new MockLogger<UserManager<ApplicationUser>>());

        var claimsFactory = new MockClaimsPrincipalFactory();

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        var signInManager = new SignInManager<ApplicationUser>(
            userManager,
            httpContextAccessor,
            claimsFactory,
            Options.Create(new IdentityOptions()),
            new MockLogger<SignInManager<ApplicationUser>>(),
            null!,
            null!);

        var jwtService = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "CareerOS.Api",
            Audience = "CareerOS.Frontend",
            SecretKey = "Super_Secret_Test_Key_For_Unit_Testing_256_Bits!",
            AccessTokenMinutes = 15
        }));

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = "test-client-id",
            ["Authentication:Google:ClientSecret"] = "test-secret",
            ["Authentication:FrontendBaseUrl"] = "http://localhost:4200"
        }).Build();

        var googleService = new GoogleLoginExchangeService(
            config,
            new System.Net.Http.HttpClient(),
            new MockLogger<GoogleLoginExchangeService>());

        var controller = new AuthController(userManager, signInManager, db, jwtService, googleService);
        return (controller, db);
    }

    [Fact]
    public async Task Register_NewUser_ReturnsCreatedWithAccessToken()
    {
        var (controller, db) = CreateController();
        var request = new RegisterRequest
        {
            Email = "newuser@test.com",
            Password = "Senha12345!",
            FullName = "New User",
            ProfessionalTitle = "Developer"
        };

        var result = await controller.Register(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<AuthResponse>(createdResult.Value);

        Assert.Equal("newuser@test.com", response.Email);
        Assert.Equal("New User", response.FullName);
        Assert.NotEmpty(response.AccessToken);
        Assert.Equal("Bearer", response.TokenType);

        var userInDb = await db.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        Assert.NotNull(userInDb);
        Assert.NotNull(userInDb.PasswordHash);
        Assert.Null(userInDb.LegacyPasswordHash);

        var profileInDb = await db.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == userInDb.CandidateProfileId);
        Assert.NotNull(profileInDb);
        Assert.Equal("New User", profileInDb.FullName);
        Assert.Equal("Developer", profileInDb.ProfessionalTitle);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        var (controller, db) = CreateController();
        var request = new RegisterRequest
        {
            Email = "dup@test.com",
            Password = "Senha12345!",
            FullName = "First User",
            ProfessionalTitle = "Dev"
        };

        await controller.Register(request);

        var duplicateRequest = new RegisterRequest
        {
            Email = "dup@test.com",
            Password = "Other12345!",
            FullName = "Second User",
            ProfessionalTitle = "QA"
        };

        var result = await controller.Register(duplicateRequest);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        var errorValue = badRequest.Value!;
        var messageProp = errorValue.GetType().GetProperty("message");
        Assert.NotNull(messageProp);
        Assert.Equal("User with this email already exists", messageProp.GetValue(errorValue));
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsValidToken()
    {
        var (controller, db) = CreateController();
        var registerRequest = new RegisterRequest
        {
            Email = "logintest@test.com",
            Password = "MyPass12345!",
            FullName = "Login Tester",
            ProfessionalTitle = "Engineer"
        };

        await controller.Register(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "logintest@test.com",
            Password = "MyPass12345!"
        };

        var loginResult = await controller.Login(loginRequest);
        var okResult = Assert.IsType<OkObjectResult>(loginResult);
        var response = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal("logintest@test.com", response.Email);
        Assert.Equal("Login Tester", response.FullName);
        Assert.NotEmpty(response.AccessToken);
    }

    private class MockClaimsPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
    {
        public Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            return Task.FromResult(new ClaimsPrincipal(identity));
        }
    }

    private class MockLogger<T> : ILogger<T> where T : class
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
