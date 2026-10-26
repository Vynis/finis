using Finis.Api.DTOs;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finis.Api.Controllers;

[Authorize]
public class EconomyController : BaseApiController
{
    private readonly ITermometroEngineService _engineService;

    public EconomyController(ITermometroEngineService engineService)
    {
        _engineService = engineService;
    }

    [HttpGet("{year:int}")]
    public async Task<ActionResult<AnnualEconomyResponseDto>> GetAnnualReport(int year)
    {
        if (year < 2000 || year > 2100)
        {
            return BadRequest(new { message = "Ano inválido." });
        }

        var userId = GetUserId();
        var report = await _engineService.GetAnnualEconomyAsync(userId, year);

        return Ok(report);
    }
}
