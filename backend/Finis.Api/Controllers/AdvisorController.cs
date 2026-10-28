using Finis.Api.DTOs;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finis.Api.Controllers;

[Authorize]
public class AdvisorController : BaseApiController
{
    private readonly IFinisAdvisorService _advisorService;

    public AdvisorController(IFinisAdvisorService advisorService)
    {
        _advisorService = advisorService;
    }

    [HttpGet("analysis")]
    public async Task<ActionResult<AdvisorAnalysisDto>> GetAnalysis(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? accountId)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;

        if (targetMonth < 1 || targetMonth > 12)
        {
            return BadRequest(new { message = "Mês inválido." });
        }

        var userId = GetUserId();
        var analysis = await _advisorService.GetAdvisorAnalysisAsync(userId, targetYear, targetMonth, accountId);

        return Ok(analysis);
    }
}
