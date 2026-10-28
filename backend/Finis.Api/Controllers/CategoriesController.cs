using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Controllers;

[Authorize]
public class CategoriesController : BaseApiController
{
    private readonly FinisDbContext _context;

    public CategoriesController(FinisDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var userId = GetUserId();

        var categories = await _context.Categories
            .Where(c => c.IsDefault || c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                ColorHex = c.ColorHex,
                Icon = c.Icon,
                IsDefault = c.IsDefault
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryDto dto)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "O nome da categoria é obrigatório." });
        }

        var category = new Category
        {
            Name = dto.Name.Trim(),
            Type = dto.Type,
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#3B82F6" : dto.ColorHex.Trim(),
            Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "tag" : dto.Icon.Trim(),
            IsDefault = false,
            UserId = userId
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        dto.Id = category.Id;
        dto.IsDefault = false;

        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryDto dto)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "O nome da categoria é obrigatório." });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == userId || c.IsDefault));

        if (category == null)
        {
            return NotFound(new { message = "Categoria não encontrada." });
        }

        category.Name = dto.Name.Trim();
        category.Type = dto.Type;
        category.ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#3B82F6" : dto.ColorHex.Trim();
        category.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "tag" : dto.Icon.Trim();

        await _context.SaveChangesAsync();

        dto.Id = category.Id;
        dto.IsDefault = category.IsDefault;

        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == userId || c.IsDefault));

        if (category == null)
        {
            return NotFound(new { message = "Categoria não encontrada." });
        }

        // Dissociate transactions before removing category to preserve financial history
        var transactions = await _context.Transactions
            .Where(t => t.CategoryId == id)
            .ToListAsync();

        foreach (var t in transactions)
        {
            t.CategoryId = null;
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
