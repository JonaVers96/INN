using LYL.Api.Contracts.Supervisor;
using LYL.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYL.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupervisorController(IConfiguration configuration, ISupervisorRepository supervisorRepository) : ControllerBase
{
    [HttpPost("verify-pin")]
    public IActionResult VerifyPin([FromBody] VerifyPinRequest request)
    {
        var configuredPin = configuration["Supervisor:Pin"];

        if (string.IsNullOrEmpty(configuredPin))
        {
            return StatusCode(500, new { message = "Supervisor PIN is not configured." });
        }

        if (string.Equals(request.Pin, configuredPin, StringComparison.Ordinal))
        {
            return Ok();
        }

        return Unauthorized();
    }
    
    [HttpGet("{supervisorId:guid}/skip-checks")]
    public async Task<IActionResult> GetSkipChecks(Guid supervisorId)
    {
        var supervisor = await supervisorRepository.GetByIdAsync(supervisorId);
        if (supervisor == null) return NotFound();

        return Ok(new { skipChecks = supervisor.SkipChecks });
    }

    [HttpPut("{supervisorId:guid}/skip-checks")]
    public async Task<IActionResult> SetSkipChecks(Guid supervisorId, [FromBody] bool skipChecks)
    {
        var supervisor = await supervisorRepository.GetByIdAsync(supervisorId);
        if (supervisor == null) return NotFound();

        supervisor.SkipChecks = skipChecks;
        return Ok(new { skipChecks });
    }
}
