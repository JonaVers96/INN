using LYL.Api.Contracts.Supervisor;
using LYL.Api.Hubs;
using LYL.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace LYL.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupervisorController(IConfiguration configuration, ISupervisorRepository supervisorRepository, IHubContext<GameHub> hubContext) : ControllerBase
{
    private static readonly string[] SupportedLanguages = ["nl", "en", "fr"];

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

    [HttpGet("{supervisorId:guid}/language")]
    public async Task<IActionResult> GetLanguage(Guid supervisorId)
    {
        var supervisor = await supervisorRepository.GetByIdAsync(supervisorId);
        if (supervisor == null) return NotFound();

        return Ok(new { language = supervisor.Language });
    }

    [HttpPut("{supervisorId:guid}/language")]
    public async Task<IActionResult> SetLanguage(Guid supervisorId, [FromBody] string language, [FromQuery] string? roomCode)
    {
        var supervisor = await supervisorRepository.GetByIdAsync(supervisorId);
        if (supervisor == null) return NotFound();

        // Enkel talen toelaten waarvoor de frontend vertalingen heeft
        if (!SupportedLanguages.Contains(language))
            return BadRequest(new { message = "Taal wordt niet ondersteund." });

        supervisor.Language = language;

        // Spelers in de kamer meteen laten wisselen van taal
        if (!string.IsNullOrEmpty(roomCode))
            await hubContext.Clients.Group(roomCode).SendAsync("LanguageChanged", language);

        return Ok(new { language });
    }

}
