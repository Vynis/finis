using Finis.Api.DTOs;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finis.Api.Controllers;

[Authorize]
public class DashboardController : BaseApiController
{
    private readonly ITermometroEngineService _engineService;

    public DashboardController(ITermometroEngineService engineService)
    {
        _engineService = engineService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary(
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
        var summary = await _engineService.GetDashboardSummaryAsync(userId, targetYear, targetMonth, accountId);

        return Ok(summary);
    }
}
