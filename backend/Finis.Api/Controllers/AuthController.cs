using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly FinisDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(FinisDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower()))
        {
            return BadRequest(new { message = "Este endereço de e-mail já está em uso." });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = passwordHash,
            InitialBalance = dto.InitialBalance,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            User = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                InitialBalance = user.InitialBalance,
                CreatedAt = user.CreatedAt
            }
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "E-mail ou senha inválidos." });
        }

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            User = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                InitialBalance = user.InitialBalance,
                CreatedAt = user.CreatedAt
            }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        var userId = GetUserId();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            InitialBalance = user.InitialBalance,
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize]
    [HttpPut("initial-balance")]
    public async Task<ActionResult<UserProfileDto>> UpdateInitialBalance([FromBody] UpdateBalanceRequestDto dto)
    {
        var userId = GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        user.InitialBalance = dto.InitialBalance;
        await _context.SaveChangesAsync();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            InitialBalance = user.InitialBalance,
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize]
    [HttpPost("reset-data")]
    public async Task<ActionResult<UserProfileDto>> ResetAllUserData()
    {
        var userId = GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Usuário não encontrado." });
        }

        // Delete all transactions of the user
        var userTransactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();
        _context.Transactions.RemoveRange(userTransactions);

        // Delete custom categories of the user (preserve default ones where IsDefault is true)
        var userCategories = await _context.Categories
            .Where(c => c.UserId == userId && !c.IsDefault)
            .ToListAsync();
        _context.Categories.RemoveRange(userCategories);

        // Delete user credit cards
        var userCards = await _context.CreditCards
            .Where(c => c.UserId == userId)
            .ToListAsync();
        _context.CreditCards.RemoveRange(userCards);

        // Reset initial balance to 0.00
        user.InitialBalance = 0.00m;

        await _context.SaveChangesAsync();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            InitialBalance = user.InitialBalance,
            CreatedAt = user.CreatedAt
        });
    }
}
