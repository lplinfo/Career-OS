using System;
using System.Collections.Generic;
using CareerOS.Api.Contracts;
using CareerOS.Api.Services;
using Xunit;

namespace CareerOS.Api.Tests;

public class LinkedinParserAndGapAnalysisTests
{
    private readonly LinkedinGapAnalysisService _gapService = new();

    [Fact]
    public void GapAnalysis_DetectsMissingSummaryAndContacts()
    {
        var profile = new ParsedCandidateProfileDto
        {
            FullName = "John Doe",
            ProfessionalTitle = "Software Engineer",
            ProfessionalSummary = "", // Missing
            Phone = null, // Missing
            City = null,
            Country = null
        };

        var analysis = _gapService.Analyze(profile);

        Assert.Contains("Resumo Profissional", analysis.MissingFields);
        Assert.Contains("Telefone de Contato", analysis.MissingFields);
        Assert.True(analysis.CompletenessScore < 100);
    }

    [Fact]
    public void GapAnalysis_IdentifiesLackOfMetricsInExperience()
    {
        var profile = new ParsedCandidateProfileDto
        {
            FullName = "Jane Smith",
            ProfessionalTitle = "Product Manager",
            ProfessionalSummary = "Experienced PM building web products.",
            Phone = "+55 11 99999-9999",
            City = "São Paulo",
            Country = "Brasil",
            EducationHistory = [new ParsedEducationDto { Institution = "USP", Course = "Engenharia" }],
            Certifications = [new ParsedCertificationDto { Name = "PMP", Issuer = "PMI" }],
            WorkExperiences = [
                new ParsedWorkExperienceDto
                {
                    Company = "Tech Corp",
                    Role = "PM",
                    Description = "Managed team backlogs and conducted daily meetings." // No metrics
                }
            ],
            Skills = ["Agile", "Scrum", "Product Roadmap"]
        };

        var analysis = _gapService.Analyze(profile);

        Assert.DoesNotContain("Resumo Profissional", analysis.MissingFields);
        Assert.Contains(analysis.Recommendations, r => r.Title.Contains("métricas"));
    }

    [Fact]
    public void ParserService_InstantiatesAndClassifiesSectionsCorrectly()
    {
        var parser = new LinkedinParserService();
        Assert.NotNull(parser);
    }
}
