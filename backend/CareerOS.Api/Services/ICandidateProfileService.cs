using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CareerOS.Api.Contracts;
using Microsoft.AspNetCore.Http;

namespace CareerOS.Api.Services;

public interface ICandidateProfileService
{
    Task<IEnumerable<CandidateProfileResponse>> GetAllAsync();
    Task<CandidateProfileResponse?> GetByIdAsync(Guid id);
    Task<(CandidateProfileResponse? Profile, string? Error, bool IsConflict)> CreateAsync(CandidateProfileRequest request);
    Task<(CandidateProfileResponse? Profile, string? Error, bool IsNotFound)> UpdateAsync(Guid id, CandidateProfileRequest request);
    Task<bool> DeleteAsync(Guid id);
    (LinkedinImportResponseDto? Result, string? Error) ImportLinkedin(IFormFile file);
}
