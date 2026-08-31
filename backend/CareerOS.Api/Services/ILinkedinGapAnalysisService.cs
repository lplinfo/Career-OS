using CareerOS.Api.Contracts;

namespace CareerOS.Api.Services;

public interface ILinkedinGapAnalysisService
{
    GapAnalysisDto Analyze(ParsedCandidateProfileDto parsedProfile);
}
