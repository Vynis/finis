using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Controllers;

[Authorize]
public class DebtsController : BaseApiController
{
    private readonly FinisDbContext _context;

    public DebtsController(FinisDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<DebtResponseDto>>> GetDebts([FromQuery] int? accountId)
    {
        var userId = GetUserId();

        var query = _context.Debts
            .Include(d => d.Account)
            .Where(d => d.UserId == userId);

        if (accountId.HasValue)
        {
            query = query.Where(d => d.AccountId == accountId.Value);
        }

        var debts = await query
            .OrderBy(d => d.IsSettled)
            .ThenByDescending(d => d.InterestRateMonthly)
            .ThenByDescending(d => d.RemainingAmount)
            .Select(d => new DebtResponseDto
            {
                Id = d.Id,
                Name = d.Name,
                Creditor = d.Creditor,
                AccountId = d.AccountId,
                AccountName = d.Account != null ? d.Account.Name : null,
                TotalAmount = d.TotalAmount,
                RemainingAmount = d.RemainingAmount,
                InterestRateMonthly = d.InterestRateMonthly,
                MonthlyPayment = d.MonthlyPayment,
                DueDay = d.DueDay,
                ColorHex = d.ColorHex,
                Notes = d.Notes,
                IsSettled = d.IsSettled,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        foreach (var debt in debts)
        {
            if (debt.MonthlyPayment > 0 && !debt.IsSettled)
            {
                // Simple payoff estimate months
                debt.EstimatedMonthsToPayoff = (int)Math.Ceiling(debt.RemainingAmount / debt.MonthlyPayment);
            }
        }

        return Ok(debts);
    }

    [HttpPost]
    public async Task<ActionResult<DebtResponseDto>> Create([FromBody] DebtCreateUpdateDto dto)
    {
        var userId = GetUserId();

        var debt = new Debt
        {
            UserId = userId,
            AccountId = dto.AccountId,
            Name = dto.Name.Trim(),
            Creditor = dto.Creditor?.Trim(),
            TotalAmount = dto.TotalAmount,
            RemainingAmount = dto.RemainingAmount,
            InterestRateMonthly = dto.InterestRateMonthly,
            MonthlyPayment = dto.MonthlyPayment,
            DueDay = dto.DueDay,
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#EF4444" : dto.ColorHex.Trim(),
            Notes = dto.Notes?.Trim(),
            IsSettled = dto.IsSettled || dto.RemainingAmount <= 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Debts.Add(debt);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDebts), new { id = debt.Id }, new DebtResponseDto
        {
            Id = debt.Id,
            Name = debt.Name,
            Creditor = debt.Creditor,
            AccountId = debt.AccountId,
            TotalAmount = debt.TotalAmount,
            RemainingAmount = debt.RemainingAmount,
            InterestRateMonthly = debt.InterestRateMonthly,
            MonthlyPayment = debt.MonthlyPayment,
            DueDay = debt.DueDay,
            ColorHex = debt.ColorHex,
            Notes = debt.Notes,
            IsSettled = debt.IsSettled,
            CreatedAt = debt.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DebtResponseDto>> Update(int id, [FromBody] DebtCreateUpdateDto dto)
    {
        var userId = GetUserId();

        var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (debt == null)
        {
            return NotFound(new { message = "Dívida não encontrada." });
        }

        debt.Name = dto.Name.Trim();
        debt.Creditor = dto.Creditor?.Trim();
        debt.AccountId = dto.AccountId;
        debt.TotalAmount = dto.TotalAmount;
        debt.RemainingAmount = dto.RemainingAmount;
        debt.InterestRateMonthly = dto.InterestRateMonthly;
        debt.MonthlyPayment = dto.MonthlyPayment;
        debt.DueDay = dto.DueDay;
        debt.ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#EF4444" : dto.ColorHex.Trim();
        debt.Notes = dto.Notes?.Trim();
        debt.IsSettled = dto.IsSettled || debt.RemainingAmount <= 0;

        await _context.SaveChangesAsync();

        return Ok(new DebtResponseDto
        {
            Id = debt.Id,
            Name = debt.Name,
            Creditor = debt.Creditor,
            AccountId = debt.AccountId,
            TotalAmount = debt.TotalAmount,
            RemainingAmount = debt.RemainingAmount,
            InterestRateMonthly = debt.InterestRateMonthly,
            MonthlyPayment = debt.MonthlyPayment,
            DueDay = debt.DueDay,
            ColorHex = debt.ColorHex,
            Notes = debt.Notes,
            IsSettled = debt.IsSettled,
            CreatedAt = debt.CreatedAt
        });
    }

    [HttpPost("{id:int}/amortize")]
    public async Task<ActionResult<DebtResponseDto>> Amortize(int id, [FromBody] DebtAmortizeDto dto)
    {
        var userId = GetUserId();

        var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (debt == null)
        {
            return NotFound(new { message = "Dívida não encontrada." });
        }

        debt.RemainingAmount = Math.Max(0, debt.RemainingAmount - dto.Amount);
        if (debt.RemainingAmount <= 0)
        {
            debt.IsSettled = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new DebtResponseDto
        {
            Id = debt.Id,
            Name = debt.Name,
            Creditor = debt.Creditor,
            AccountId = debt.AccountId,
            TotalAmount = debt.TotalAmount,
            RemainingAmount = debt.RemainingAmount,
            InterestRateMonthly = debt.InterestRateMonthly,
            MonthlyPayment = debt.MonthlyPayment,
            DueDay = debt.DueDay,
            ColorHex = debt.ColorHex,
            Notes = debt.Notes,
            IsSettled = debt.IsSettled,
            CreatedAt = debt.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (debt == null)
        {
            return NotFound(new { message = "Dívida não encontrada." });
        }

        _context.Debts.Remove(debt);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
