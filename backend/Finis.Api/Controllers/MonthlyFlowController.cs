using Finis.Api.DTOs;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finis.Api.Controllers;

[Authorize]
public class MonthlyFlowController : BaseApiController
{
    private readonly ITermometroEngineService _engineService;

    public MonthlyFlowController(ITermometroEngineService engineService)
    {
        _engineService = engineService;
    }

    [HttpGet("{year:int}/{month:int}")]
    public async Task<ActionResult<MonthlyFlowResponseDto>> GetMonthlyFlow(int year, int month, [FromQuery] int? accountId)
    {
        if (month < 1 || month > 12)
        {
            return BadRequest(new { message = "Mês inválido. Deve ser entre 1 e 12." });
        }

        if (year < 2000 || year > 2100)
        {
            return BadRequest(new { message = "Ano fora do intervalo permitido." });
        }

        var userId = GetUserId();
        var result = await _engineService.GetMonthlyFlowAsync(userId, year, month, accountId);

        return Ok(result);
    }
}
