using System.Security.Claims;
using CareerOS.Api.Contracts;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using CareerOS.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    CareerDbContext dbContext,
    IJwtTokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists" });
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync<IActionResult>(async () =>
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var profile = new CandidateProfile
                {
                    Id = Guid.NewGuid(),
                    FullName = request.FullName,
                    ProfessionalTitle = request.ProfessionalTitle,
                    Email = request.Email,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                dbContext.CandidateProfiles.Add(profile);
                await dbContext.SaveChangesAsync();

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = request.Email,
                    Email = request.Email,
                    CandidateProfileId = profile.Id,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(createResult.Errors);
                }

                await transaction.CommitAsync();

                var (token, expiresAt) = tokenService.GenerateAccessToken(user);

                var response = new AuthResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    CandidateProfileId = profile.Id,
                    FullName = profile.FullName,
                    AccessToken = token,
                    TokenType = "Bearer",
                    ExpiresAt = expiresAt
                };

                return CreatedAtAction(nameof(Me), response);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (!string.IsNullOrEmpty(user.LegacyPasswordHash) &&
                PasswordHasher.VerifyPassword(request.Password, user.LegacyPasswordHash))
            {
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    await userManager.RemovePasswordAsync(user);
                }

                var addPasswordResult = await userManager.AddPasswordAsync(user, request.Password);
                if (addPasswordResult.Succeeded)
                {
                    user.LegacyPasswordHash = null;
                    await userManager.UpdateAsync(user);

                    var legacyProfile = await dbContext.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == user.CandidateProfileId);
                    var (legacyToken, legacyExpiresAt) = tokenService.GenerateAccessToken(user);

                    var legacyResponse = new AuthResponse
                    {
                        UserId = user.Id,
                        Email = user.Email ?? request.Email,
                        CandidateProfileId = user.CandidateProfileId,
                        FullName = legacyProfile?.FullName ?? string.Empty,
                        AccessToken = legacyToken,
                        TokenType = "Bearer",
                        ExpiresAt = legacyExpiresAt
                    };

                    return Ok(legacyResponse);
                }
            }

            return Unauthorized(new { message = "Invalid email or password" });
        }

        var profile = await dbContext.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == user.CandidateProfileId);
        var (token, expiresAt) = tokenService.GenerateAccessToken(user);

        var response = new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email ?? request.Email,
            CandidateProfileId = user.CandidateProfileId,
            FullName = profile?.FullName ?? string.Empty,
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt
        };

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Unauthorized();
        }

        var profile = await dbContext.CandidateProfiles.FirstOrDefaultAsync(p => p.Id == user.CandidateProfileId);

        string currentToken = string.Empty;
        var authHeader = Request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            currentToken = authHeader.Substring("Bearer ".Length).Trim();
        }

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow;
        var expClaim = User.FindFirstValue("exp");
        if (long.TryParse(expClaim, out var expUnix))
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
        }

        var response = new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            CandidateProfileId = user.CandidateProfileId,
            FullName = profile?.FullName ?? string.Empty,
            AccessToken = currentToken,
            TokenType = "Bearer",
            ExpiresAt = expiresAt
        };

        return Ok(response);
    }
}
