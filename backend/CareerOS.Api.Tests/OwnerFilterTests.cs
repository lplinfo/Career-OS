using CareerOS.Api.Contracts;
using CareerOS.Api.Controllers;
using CareerOS.Api.Data;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CareerOS.Api.Tests;

public class TestCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }
    public Guid? CandidateProfileId { get; set; }
    public bool IsAuthenticated { get; set; } = true;
}

public class OwnerFilterTests
{
    private static CareerDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CareerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CareerDbContext(options);
    }

    [Fact]
    public async Task CandidateProfilesController_GetAll_ReturnsOnlyUserProfile()
    {
        using var db = GetInMemoryDbContext(nameof(CandidateProfilesController_GetAll_ReturnsOnlyUserProfile));
        var userProfileId = Guid.NewGuid();
        var otherProfileId = Guid.NewGuid();

        db.CandidateProfiles.AddRange(
            new CandidateProfile { Id = userProfileId, FullName = "Alice", ProfessionalTitle = "Dev", Email = "alice@example.com" },
            new CandidateProfile { Id = otherProfileId, FullName = "Bob", ProfessionalTitle = "PM", Email = "bob@example.com" }
        );
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUser { CandidateProfileId = userProfileId };
        var service = new CandidateProfileService(db, currentUser, new LinkedinParserService(), new LinkedinGapAnalysisService());
        var controller = new CandidateProfilesController(service);

        var actionResult = await controller.GetAll();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var profiles = Assert.IsAssignableFrom<IEnumerable<CandidateProfileResponse>>(okResult.Value);

        var singleProfile = Assert.Single(profiles);
        Assert.Equal(userProfileId, singleProfile.Id);
    }

    [Fact]
    public async Task CandidateProfilesController_Get_ReturnsNotFoundForOtherUser()
    {
        using var db = GetInMemoryDbContext(nameof(CandidateProfilesController_Get_ReturnsNotFoundForOtherUser));
        var userProfileId = Guid.NewGuid();
        var otherProfileId = Guid.NewGuid();

        db.CandidateProfiles.Add(new CandidateProfile { Id = otherProfileId, FullName = "Bob", ProfessionalTitle = "PM", Email = "bob@example.com" });
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUser { CandidateProfileId = userProfileId };
        var service = new CandidateProfileService(db, currentUser, new LinkedinParserService(), new LinkedinGapAnalysisService());
        var controller = new CandidateProfilesController(service);

        var actionResult = await controller.Get(otherProfileId);
        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task CandidateProfilesController_Create_ReturnsConflictIfUserAlreadyHasProfile()
    {
        using var db = GetInMemoryDbContext(nameof(CandidateProfilesController_Create_ReturnsConflictIfUserAlreadyHasProfile));
        var userProfileId = Guid.NewGuid();

        var currentUser = new TestCurrentUser { CandidateProfileId = userProfileId };
        var service = new CandidateProfileService(db, currentUser, new LinkedinParserService(), new LinkedinGapAnalysisService());
        var controller = new CandidateProfilesController(service);

        var request = new CandidateProfileRequest
        {
            FullName = "Alice",
            ProfessionalTitle = "Dev",
            Email = "alice@example.com"
        };

        var actionResult = await controller.Create(request);
        Assert.IsType<ConflictObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task ResumesController_GetAll_ReturnsOnlyUserResumes()
    {
        using var db = GetInMemoryDbContext(nameof(ResumesController_GetAll_ReturnsOnlyUserResumes));
        var userProfileId = Guid.NewGuid();
        var otherProfileId = Guid.NewGuid();

        db.Resumes.AddRange(
            new Resume { Id = Guid.NewGuid(), CandidateProfileId = userProfileId, Language = "pt-BR", CustomizedTitle = "Software Engineer", Skills = "C#, .NET", CustomizedSummary = "Summary A", TargetCountry = "BR" },
            new Resume { Id = Guid.NewGuid(), CandidateProfileId = otherProfileId, Language = "en-US", CustomizedTitle = "Product Manager", Skills = "Agile", CustomizedSummary = "Summary B", TargetCountry = "US" }
        );
        await db.SaveChangesAsync();

        var currentUser = new TestCurrentUser { CandidateProfileId = userProfileId };
        var service = new ResumeService(db, currentUser);
        var controller = new ResumesController(service);

        var actionResult = await controller.GetAll();
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var resumes = Assert.IsAssignableFrom<IEnumerable<ResumeResponse>>(okResult.Value);

        var singleResume = Assert.Single(resumes);
        Assert.Equal(userProfileId, singleResume.CandidateProfileId);
    }

    [Fact]
    public async Task ResumesController_Create_OverridesBodyCandidateProfileIdWithUserIdentity()
    {
        using var db = GetInMemoryDbContext(nameof(ResumesController_Create_OverridesBodyCandidateProfileIdWithUserIdentity));
        var userProfileId = Guid.NewGuid();
        var maliciousProfileId = Guid.NewGuid();

        var currentUser = new TestCurrentUser { CandidateProfileId = userProfileId };
        var service = new ResumeService(db, currentUser);
        var controller = new ResumesController(service);

        var request = new ResumeRequest
        {
            CandidateProfileId = maliciousProfileId, // Attempting to assign another profile ID
            Language = "en-US",
            CustomizedTitle = "Security Analyst",
            Skills = "AppSec",
            CustomizedSummary = "Summary",
            TargetCountry = "US"
        };

        var actionResult = await controller.Create(request);
        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        var createdResume = Assert.IsType<ResumeResponse>(createdResult.Value);

        Assert.Equal(userProfileId, createdResume.CandidateProfileId);
    }
}
