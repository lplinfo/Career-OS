using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CareerOS.Api.Contracts;

namespace CareerOS.Api.Services;

public interface IResumeService
{
    Task<IEnumerable<ResumeResponse>> GetAllAsync();
    Task<ResumeResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<ResumeResponse>?> GetByCandidateAsync(Guid candidateProfileId);
    Task<(ResumeResponse? Resume, string? Error)> CreateAsync(ResumeRequest request);
    Task<(ResumeResponse? Resume, string? Error, bool IsNotFound)> UpdateAsync(Guid id, ResumeRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<(byte[] FileBytes, string FileName, string ContentType)?> ExportPdfAsync(Guid id);
    Task<(byte[] FileBytes, string FileName, string ContentType)?> ExportDocxAsync(Guid id);
    Task<(string Content, string ContentType)?> ExportAtsAsync(Guid id);
}
