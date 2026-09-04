using CareerOS.Api.Contracts;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareerOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/resumes")]
public class ResumesController(IResumeService resumeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResumeResponse>>> GetAll()
    {
        var resumes = await resumeService.GetAllAsync();
        return Ok(resumes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResumeResponse>> Get(Guid id)
    {
        var resume = await resumeService.GetByIdAsync(id);
        return resume is not null ? Ok(resume) : NotFound();
    }

    [HttpGet("by-candidate/{candidateProfileId:guid}")]
    public async Task<ActionResult<IEnumerable<ResumeResponse>>> GetByCandidate(Guid candidateProfileId)
    {
        var resumes = await resumeService.GetByCandidateAsync(candidateProfileId);
        return resumes is not null ? Ok(resumes) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<ResumeResponse>> Create(ResumeRequest request)
    {
        var (resume, error) = await resumeService.CreateAsync(request);
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(Get), new { id = resume!.Id }, resume);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ResumeResponse>> Update(Guid id, ResumeRequest request)
    {
        var (resume, error, isNotFound) = await resumeService.UpdateAsync(id, request);
        if (isNotFound)
        {
            return NotFound();
        }
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(resume);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await resumeService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var export = await resumeService.ExportPdfAsync(id);
        if (export is null) return NotFound();

        return File(export.Value.FileBytes, export.Value.ContentType, export.Value.FileName);
    }

    [HttpGet("{id:guid}/export/docx")]
    public async Task<IActionResult> ExportDocx(Guid id)
    {
        var export = await resumeService.ExportDocxAsync(id);
        if (export is null) return NotFound();

        return File(export.Value.FileBytes, export.Value.ContentType, export.Value.FileName);
    }

    [HttpGet("{id:guid}/export/ats")]
    public async Task<IActionResult> ExportAts(Guid id)
    {
        var export = await resumeService.ExportAtsAsync(id);
        if (export is null) return NotFound();

        return Content(export.Value.Content, export.Value.ContentType, System.Text.Encoding.UTF8);
    }
}
