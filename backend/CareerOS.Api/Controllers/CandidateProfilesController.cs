using CareerOS.Api.Contracts;
using CareerOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareerOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/candidate-profiles")]
public class CandidateProfilesController(ICandidateProfileService candidateProfileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateProfileResponse>>> GetAll()
    {
        var profiles = await candidateProfileService.GetAllAsync();
        return Ok(profiles);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CandidateProfileResponse>> Get(Guid id)
    {
        var profile = await candidateProfileService.GetByIdAsync(id);
        return profile is not null ? Ok(profile) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<CandidateProfileResponse>> Create(CandidateProfileRequest request)
    {
        var (profile, error, isConflict) = await candidateProfileService.CreateAsync(request);
        if (isConflict)
        {
            return Conflict(new { message = error });
        }
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return CreatedAtAction(nameof(Get), new { id = profile!.Id }, profile);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CandidateProfileResponse>> Update(Guid id, CandidateProfileRequest request)
    {
        var (profile, error, isNotFound) = await candidateProfileService.UpdateAsync(id, request);
        if (isNotFound)
        {
            return NotFound();
        }
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(profile);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await candidateProfileService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("import-linkedin")]
    public ActionResult<LinkedinImportResponseDto> ImportLinkedin(IFormFile file)
    {
        var (result, error) = candidateProfileService.ImportLinkedin(file);
        if (error != null)
        {
            return BadRequest(new { message = error });
        }

        return Ok(result);
    }
}
