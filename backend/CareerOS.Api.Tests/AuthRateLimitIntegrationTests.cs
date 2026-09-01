using System.Net;
using CareerOS.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareerOS.Api.Tests;

public class AuthRateLimitIntegrationTests
{
    private static HttpClient CreateClient() =>
        new AuthRateLimitWebFactory().CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    [Fact]
    public async Task LoginEndpoint_ExceedingLimit_ReturnsTooManyRequests()
    {
        using var client = CreateClient();

        var request = new StringContent(
            "{\"email\":\"nonexistent@test.com\",\"password\":\"WrongPass123!\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        for (var i = 0; i < 10; i++)
        {
            var response = await client.PostAsync("/api/auth/login", request);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                $"Expected Unauthorized for request {i + 1} but got {response.StatusCode}");
        }

        var rateLimited = await client.PostAsync("/api/auth/login", request);
        Assert.Equal(HttpStatusCode.TooManyRequests, rateLimited.StatusCode);
    }

    [Fact]
    public async Task Login_RepeatedWrongPassword_TriggersAccountLockout()
    {
        using var client = CreateClient();

        var registerPayload = "{\"email\":\"lockout@test.com\",\"password\":\"ValidPass123!\"," +
                              "\"fullName\":\"Lockout Tester\",\"professionalTitle\":\"Engineer\"}";
        var registerResponse = await client.PostAsync("/api/auth/register",
            new StringContent(registerPayload, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var wrongLogin = new StringContent(
            "{\"email\":\"lockout@test.com\",\"password\":\"WrongPass123!\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsync("/api/auth/login", wrongLogin);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                $"Expected Unauthorized for attempt {i + 1} but got {response.StatusCode}");
        }

        var lockoutLogin = await client.PostAsync("/api/auth/login",
            new StringContent("{\"email\":\"lockout@test.com\",\"password\":\"ValidPass123!\"}",
                System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, lockoutLogin.StatusCode);
    }
}

public class AuthRateLimitWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("JwtOptions:SecretKey", "Super_Secret_Test_Key_For_Unit_Testing_256_Bits!");
        builder.UseSetting("JwtOptions:Issuer", "CareerOS.Api");
        builder.UseSetting("JwtOptions:Audience", "CareerOS.Frontend");
        builder.UseSetting("JwtOptions:AccessTokenMinutes", "15");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(
                d => d.ServiceType == typeof(DbContextOptions<CareerDbContext>));
            services.Remove(descriptor);

            services.AddSingleton(sp =>
            {
                var name = $"AuthRateLimitTestDb_{Guid.NewGuid()}";
                return new DbContextOptionsBuilder<CareerDbContext>()
                    .UseInMemoryDatabase(name)
                    .ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                    .Options;
            });
        });
    }
}
