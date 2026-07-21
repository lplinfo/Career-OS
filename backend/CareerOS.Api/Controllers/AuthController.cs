using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareerOS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(CareerDbContext context) : ControllerBase
{
    public class RegisterRequest
    {
        [Required, EmailAddress, MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(160)]
        public string ProfessionalTitle { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public Guid CandidateProfileId { get; set; }
        public string FullName { get; set; } = string.Empty;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check if user already exists
        var exists = await context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (exists)
        {
            return BadRequest(new { message = "Este e-mail já está cadastrado." });
        }

        // Create initial candidate profile
        var profile = new CandidateProfile
        {
            FullName = request.FullName,
            Email = request.Email,
            ProfessionalTitle = request.ProfessionalTitle,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.CandidateProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Create corresponding user account
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            CandidateProfileId = profile.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            CandidateProfileId = user.CandidateProfileId,
            FullName = profile.FullName
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
        if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "E-mail ou senha incorretos." });
        }

        var profile = await context.CandidateProfiles.FindAsync(user.CandidateProfileId);
        var fullName = profile?.FullName ?? "Candidato";

        return Ok(new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            CandidateProfileId = user.CandidateProfileId,
            FullName = fullName
        });
    }
}
