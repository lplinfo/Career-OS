using System.Text.Json;
using CareerOS.Api.Domain;
using CareerOS.Api.Services;
using Xunit;

namespace CareerOS.Api.Tests;

public class ExportServiceTests
{
    [Theory]
    [InlineData("en", "PROFESSIONAL SUMMARY", "Bachelor of Science in Computer Science")]
    [InlineData("it", "RIEPILOGO PROFESSIONALE", "Bachelor of Science in Computer Science")]
    [InlineData("pt", "RESUMO PROFISSIONAL", "Bachelor of Science em Computer Science")]
    public void GenerateAtsText_UsesLocalizedHeadersAndCustomizedCollections(string language, string expectedSummaryHeader, string expectedEducationText)
    {
        var profile = CreateProfile();
        var resume = CreateResume(language);
        resume.CustomizedExperiencesJson = JsonSerializer.Serialize(new[]
        {
            new WorkExperience
            {
                Role = "Customized Engineering Lead",
                Company = "Tailored Labs",
                Description = "Led a customized platform migration.",
                StartDate = new DateOnly(2023, 1, 1),
                IsCurrent = true,
                DisplayOrder = 1
            }
        });
        resume.CustomizedEducationsJson = JsonSerializer.Serialize(profile.EducationHistory);
        resume.CustomizedCertificationsJson = JsonSerializer.Serialize(profile.Certifications);

        var result = ExportService.GenerateAtsText(resume, profile);

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains(profile.FullName.ToUpperInvariant(), result);
        Assert.Contains(expectedSummaryHeader, result);
        Assert.Contains("Customized Engineering Lead", result);
        Assert.Contains(expectedEducationText, result);
        Assert.DoesNotContain("Profile Software Engineer", result);
    }

    [Fact]
    public void GenerateAtsText_FallsBackToProfileCollectionsWhenNoCustomizedJsonExists()
    {
        var profile = CreateProfile();
        var resume = CreateResume("pt");

        var result = ExportService.GenerateAtsText(resume, profile);

        Assert.Contains("EXPERIÊNCIA PROFISSIONAL", result);
        Assert.Contains("Profile Software Engineer", result);
        Assert.Contains("University of Sao Paulo", result);
        Assert.Contains("Bachelor of Science em Computer Science", result);
        Assert.Contains("AWS Certified Developer", result);
    }

    [Fact]
    public void GenerateAtsText_FallsBackToProfileCollectionsWhenCustomizedJsonIsMalformed()
    {
        var profile = CreateProfile();
        var resume = CreateResume("en");
        resume.CustomizedExperiencesJson = "{not valid json";
        resume.CustomizedEducationsJson = "[invalid";
        resume.CustomizedCertificationsJson = "not-json";

        var result = ExportService.GenerateAtsText(resume, profile);

        Assert.Contains("PROFESSIONAL EXPERIENCE", result);
        Assert.Contains("Profile Software Engineer", result);
        Assert.Contains("University of Sao Paulo", result);
        Assert.Contains("AWS Certified Developer", result);
    }

    [Theory]
    [InlineData("pt", "Bachelor of Science em Computer Science")]
    [InlineData("en", "Bachelor of Science in Computer Science")]
    [InlineData("it", "Bachelor of Science in Computer Science")]
    public void GenerateDocx_ReturnsNonEmptyOpenXmlPackageForEachLanguage(string language, string expectedEducationText)
    {
        var document = ExportService.GenerateDocx(CreateResume(language), CreateProfile());

        Assert.NotEmpty(document);
        Assert.True(document.Length > 2);
        Assert.Equal((byte)'P', document[0]);
        Assert.Equal((byte)'K', document[1]);

        using var memStream = new System.IO.MemoryStream(document);
        using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(memStream, false);
        var fullText = wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        Assert.Contains(expectedEducationText, fullText);
    }

    [Theory]
    [InlineData("pt", "Bachelor of Science em Computer Science")]
    [InlineData("en", "Bachelor of Science in Computer Science")]
    [InlineData("it", "Bachelor of Science in Computer Science")]
    public void GeneratePdf_ReturnsNonEmptyPdfForEachLanguage(string language, string expectedEducationText)
    {
        var document = ExportService.GeneratePdf(CreateResume(language), CreateProfile());

        Assert.NotEmpty(document);
        Assert.True(document.Length > 4);
        Assert.Equal((byte)'%', document[0]);
        Assert.Equal((byte)'P', document[1]);
        Assert.Equal((byte)'D', document[2]);
        Assert.Equal((byte)'F', document[3]);

        using var pdfDoc = UglyToad.PdfPig.PdfDocument.Open(document);
        var extractedText = string.Join(" ", pdfDoc.GetPages().Select(p => p.Text));
        Assert.Contains(expectedEducationText, extractedText);
    }

    private static Resume CreateResume(string language) => new()
    {
        Language = language,
        CustomizedTitle = "Senior Software Engineer",
        CustomizedSummary = "Backend engineer focused on resilient distributed systems.",
        Skills = "C#, ASP.NET Core, PostgreSQL, Docker",
        ShowEmail = true,
        ShowPhone = true,
        ShowLocation = true
    };

    private static CandidateProfile CreateProfile() => new()
    {
        FullName = "Ana Martins",
        ProfessionalTitle = "Profile Software Engineer",
        ProfessionalSummary = "Profile summary that is used when no customization is supplied.",
        Email = "ana.martins@example.com",
        Phone = "+55 11 99999-9999",
        City = "Sao Paulo",
        Region = "SP",
        Country = "Brazil",
        WorkExperiences =
        [
            new WorkExperience
            {
                Role = "Profile Software Engineer",
                Company = "CareerOS",
                Description = "Built APIs and cloud-native services.",
                StartDate = new DateOnly(2021, 2, 1),
                EndDate = new DateOnly(2022, 12, 1),
                DisplayOrder = 2
            },
            new WorkExperience
            {
                Role = "Junior Developer",
                Company = "Initial Tech",
                Description = "Delivered internal business applications.",
                StartDate = new DateOnly(2019, 1, 1),
                EndDate = new DateOnly(2021, 1, 1),
                DisplayOrder = 1
            }
        ],
        EducationHistory =
        [
            new Education
            {
                Degree = "Bachelor of Science",
                Course = "Computer Science",
                Institution = "University of Sao Paulo",
                CompletionDate = new DateOnly(2018, 12, 1),
                DisplayOrder = 1
            }
        ],
        Certifications =
        [
            new Certification
            {
                Name = "AWS Certified Developer",
                Issuer = "Amazon Web Services",
                IssuedAt = new DateOnly(2024, 4, 1),
                CredentialUrl = "https://aws.amazon.com/verification/AWS-12345",
                DisplayOrder = 1
            }
        ]
    };
}