using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Controllers;

[Authorize]
public class GoalsController : BaseApiController
{
    private readonly FinisDbContext _context;

    public GoalsController(FinisDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<GoalResponseDto>>> GetGoals([FromQuery] int? accountId)
    {
        var userId = GetUserId();

        var query = _context.Goals
            .Include(g => g.Account)
            .Where(g => g.UserId == userId);

        if (accountId.HasValue)
        {
            query = query.Where(g => g.AccountId == accountId.Value);
        }

        var goals = await query
            .OrderBy(g => g.IsCompleted)
            .ThenBy(g => g.Priority)
            .ThenBy(g => g.TargetDate)
            .Select(g => new GoalResponseDto
            {
                Id = g.Id,
                Name = g.Name,
                AccountId = g.AccountId,
                AccountName = g.Account != null ? g.Account.Name : null,
                TargetAmount = g.TargetAmount,
                CurrentAmount = g.CurrentAmount,
                TargetDate = g.TargetDate,
                Category = g.Category,
                ColorHex = g.ColorHex,
                Icon = g.Icon,
                Priority = g.Priority,
                IsCompleted = g.IsCompleted,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        var now = DateTime.Today;
        foreach (var goal in goals)
        {
            if (goal.TargetDate.HasValue && !goal.IsCompleted)
            {
                var diff = ((goal.TargetDate.Value.Year - now.Year) * 12) + (goal.TargetDate.Value.Month - now.Month);
                int months = Math.Max(1, diff);
                goal.MonthsRemaining = months;
                goal.MonthlyRequiredSavings = Math.Round(goal.RemainingAmount / months, 2);
            }
        }

        return Ok(goals);
    }

    [HttpPost]
    public async Task<ActionResult<GoalResponseDto>> Create([FromBody] GoalCreateUpdateDto dto)
    {
        var userId = GetUserId();

        var goal = new Goal
        {
            UserId = userId,
            AccountId = dto.AccountId,
            Name = dto.Name.Trim(),
            TargetAmount = dto.TargetAmount,
            CurrentAmount = dto.CurrentAmount,
            TargetDate = dto.TargetDate,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "Geral" : dto.Category.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#10B981" : dto.ColorHex.Trim(),
            Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "bullseye" : dto.Icon.Trim(),
            Priority = dto.Priority,
            IsCompleted = dto.IsCompleted || dto.CurrentAmount >= dto.TargetAmount,
            CreatedAt = DateTime.UtcNow
        };

        _context.Goals.Add(goal);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGoals), new { id = goal.Id }, new GoalResponseDto
        {
            Id = goal.Id,
            Name = goal.Name,
            AccountId = goal.AccountId,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            TargetDate = goal.TargetDate,
            Category = goal.Category,
            ColorHex = goal.ColorHex,
            Icon = goal.Icon,
            Priority = goal.Priority,
            IsCompleted = goal.IsCompleted,
            CreatedAt = goal.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<GoalResponseDto>> Update(int id, [FromBody] GoalCreateUpdateDto dto)
    {
        var userId = GetUserId();

        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
        {
            return NotFound(new { message = "Meta não encontrada." });
        }

        goal.Name = dto.Name.Trim();
        goal.AccountId = dto.AccountId;
        goal.TargetAmount = dto.TargetAmount;
        goal.CurrentAmount = dto.CurrentAmount;
        goal.TargetDate = dto.TargetDate;
        goal.Category = string.IsNullOrWhiteSpace(dto.Category) ? "Geral" : dto.Category.Trim();
        goal.ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#10B981" : dto.ColorHex.Trim();
        goal.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "bullseye" : dto.Icon.Trim();
        goal.Priority = dto.Priority;
        goal.IsCompleted = dto.IsCompleted || goal.CurrentAmount >= goal.TargetAmount;

        await _context.SaveChangesAsync();

        return Ok(new GoalResponseDto
        {
            Id = goal.Id,
            Name = goal.Name,
            AccountId = goal.AccountId,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            TargetDate = goal.TargetDate,
            Category = goal.Category,
            ColorHex = goal.ColorHex,
            Icon = goal.Icon,
            Priority = goal.Priority,
            IsCompleted = goal.IsCompleted,
            CreatedAt = goal.CreatedAt
        });
    }

    [HttpPost("{id:int}/deposit")]
    public async Task<ActionResult<GoalResponseDto>> DepositOrWithdraw(int id, [FromBody] GoalContributionDto dto)
    {
        var userId = GetUserId();

        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
        {
            return NotFound(new { message = "Meta não encontrada." });
        }

        if (dto.IsWithdrawal)
        {
            goal.CurrentAmount = Math.Max(0, goal.CurrentAmount - dto.Amount);
            if (goal.CurrentAmount < goal.TargetAmount)
            {
                goal.IsCompleted = false;
            }
        }
        else
        {
            goal.CurrentAmount += dto.Amount;
            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                goal.IsCompleted = true;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new GoalResponseDto
        {
            Id = goal.Id,
            Name = goal.Name,
            AccountId = goal.AccountId,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            TargetDate = goal.TargetDate,
            Category = goal.Category,
            ColorHex = goal.ColorHex,
            Icon = goal.Icon,
            Priority = goal.Priority,
            IsCompleted = goal.IsCompleted,
            CreatedAt = goal.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        var goal = await _context.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

        if (goal == null)
        {
            return NotFound(new { message = "Meta não encontrada." });
        }

        _context.Goals.Remove(goal);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
