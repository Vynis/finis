using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Controllers;

[Authorize]
public class AccountsController : BaseApiController
{
    private readonly FinisDbContext _context;

    public AccountsController(FinisDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> GetAccounts()
    {
        var userId = GetUserId();
        var accounts = await _context.Accounts
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Name)
            .ToListAsync();

        var result = new List<AccountResponseDto>();

        foreach (var a in accounts)
        {
            var income = await _context.Transactions
                .Where(t => t.UserId == userId && t.AccountId == a.Id && t.Type == TransactionType.Entrada)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var expenses = await _context.Transactions
                .Where(t => t.UserId == userId && t.AccountId == a.Id &&
                            (t.Type == TransactionType.SaidaFixa || t.Type == TransactionType.Diario) &&
                            (t.PaymentMethod != PaymentMethod.CartaoCredito || t.IsInvoicePayment))
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var txCount = await _context.Transactions
                .CountAsync(t => t.UserId == userId && t.AccountId == a.Id);

            result.Add(new AccountResponseDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                InitialBalance = a.InitialBalance,
                CurrentBalance = a.InitialBalance + income - expenses,
                ColorHex = a.ColorHex,
                Icon = a.Icon,
                IsActive = a.IsActive,
                TransactionsCount = txCount,
                CreatedAt = a.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccountResponseDto>> GetById(int id)
    {
        var userId = GetUserId();
        var a = await _context.Accounts
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (a == null)
        {
            return NotFound(new { message = "Conta não encontrada." });
        }

        var income = await _context.Transactions
            .Where(t => t.UserId == userId && t.AccountId == a.Id && t.Type == TransactionType.Entrada)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var expenses = await _context.Transactions
            .Where(t => t.UserId == userId && t.AccountId == a.Id &&
                        (t.Type == TransactionType.SaidaFixa || t.Type == TransactionType.Diario) &&
                        (t.PaymentMethod != PaymentMethod.CartaoCredito || t.IsInvoicePayment))
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var txCount = await _context.Transactions
            .CountAsync(t => t.UserId == userId && t.AccountId == a.Id);

        return Ok(new AccountResponseDto
        {
            Id = a.Id,
            Name = a.Name,
            Type = a.Type,
            InitialBalance = a.InitialBalance,
            CurrentBalance = a.InitialBalance + income - expenses,
            ColorHex = a.ColorHex,
            Icon = a.Icon,
            IsActive = a.IsActive,
            TransactionsCount = txCount,
            CreatedAt = a.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] AccountCreateDto dto)
    {
        var userId = GetUserId();

        var account = new Account
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = dto.Type,
            InitialBalance = dto.InitialBalance,
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#10B981" : dto.ColorHex.Trim(),
            Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "wallet2" : dto.Icon.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            InitialBalance = account.InitialBalance,
            CurrentBalance = account.InitialBalance,
            ColorHex = account.ColorHex,
            Icon = account.Icon,
            IsActive = account.IsActive,
            TransactionsCount = 0,
            CreatedAt = account.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AccountResponseDto>> Update(int id, [FromBody] AccountUpdateDto dto)
    {
        var userId = GetUserId();
        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return NotFound(new { message = "Conta não encontrada." });
        }

        account.Name = dto.Name.Trim();
        account.Type = dto.Type;
        account.InitialBalance = dto.InitialBalance;
        account.ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#10B981" : dto.ColorHex.Trim();
        account.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "wallet2" : dto.Icon.Trim();
        account.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        var income = await _context.Transactions
            .Where(t => t.UserId == userId && t.AccountId == account.Id && t.Type == TransactionType.Entrada)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var expenses = await _context.Transactions
            .Where(t => t.UserId == userId && t.AccountId == account.Id &&
                        (t.Type == TransactionType.SaidaFixa || t.Type == TransactionType.Diario) &&
                        (t.PaymentMethod != PaymentMethod.CartaoCredito || t.IsInvoicePayment))
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var txCount = await _context.Transactions
            .CountAsync(t => t.UserId == userId && t.AccountId == account.Id);

        return Ok(new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            InitialBalance = account.InitialBalance,
            CurrentBalance = account.InitialBalance + income - expenses,
            ColorHex = account.ColorHex,
            Icon = account.Icon,
            IsActive = account.IsActive,
            TransactionsCount = txCount,
            CreatedAt = account.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var activeAccountsCount = await _context.Accounts
            .CountAsync(a => a.UserId == userId && a.IsActive);

        if (activeAccountsCount <= 1)
        {
            return BadRequest(new { message = "Você precisa manter pelo menos uma conta ativa." });
        }

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (account == null)
        {
            return NotFound(new { message = "Conta não encontrada." });
        }

        var hasTransactions = await _context.Transactions
            .AnyAsync(t => t.AccountId == id && t.UserId == userId);

        if (hasTransactions)
        {
            // Soft delete
            account.IsActive = false;
        }
        else
        {
            _context.Accounts.Remove(account);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
