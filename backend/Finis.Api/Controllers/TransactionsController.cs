using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Controllers;

[Authorize]
public class TransactionsController : BaseApiController
{
    private readonly FinisDbContext _context;
    private readonly ICreditCardService _cardService;

    public TransactionsController(FinisDbContext context, ICreditCardService cardService)
    {
        _context = context;
        _cardService = cardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetTransactions(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] TransactionType? type,
        [FromQuery] int? categoryId,
        [FromQuery] int? creditCardId)
    {
        var userId = GetUserId();
        var query = _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId);

        if (year.HasValue)
        {
            query = query.Where(t => t.Date.Year == year.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(t => t.Date.Month == month.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        if (creditCardId.HasValue)
        {
            query = query.Where(t => t.CreditCardId == creditCardId.Value);
        }

        var list = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Select(t => new TransactionResponseDto
            {
                Id = t.Id,
                Date = t.Date,
                Day = t.Day,
                Month = t.Month,
                Year = t.Year,
                Type = t.Type,
                Amount = t.Amount,
                Description = t.Description,
                CategoryId = t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : "Sem categoria",
                CategoryColor = t.Category != null ? t.Category.ColorHex : "#6B7280",
                CategoryIcon = t.Category != null ? t.Category.Icon : "tag",
                Status = t.Status,
                PaymentMethod = t.PaymentMethod,
                Notes = t.Notes,
                CreditCardId = t.CreditCardId,
                CreditCardName = t.CreditCard != null ? t.CreditCard.Name : null,
                InstallmentNumber = t.InstallmentNumber,
                TotalInstallments = t.TotalInstallments,
                InvoiceMonth = t.InvoiceMonth,
                InvoiceYear = t.InvoiceYear,
                IsInvoicePayment = t.IsInvoicePayment,
                RecurrenceGroupId = t.RecurrenceGroupId,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponseDto>> GetById(int id)
    {
        var userId = GetUserId();
        var t = await _context.Transactions
            .Include(x => x.Category)
            .Include(x => x.CreditCard)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (t == null)
        {
            return NotFound(new { message = "Transação não encontrada." });
        }

        return Ok(new TransactionResponseDto
        {
            Id = t.Id,
            Date = t.Date,
            Day = t.Day,
            Month = t.Month,
            Year = t.Year,
            Type = t.Type,
            Amount = t.Amount,
            Description = t.Description,
            CategoryId = t.CategoryId,
            CategoryName = t.Category != null ? t.Category.Name : "Sem categoria",
            CategoryColor = t.Category != null ? t.Category.ColorHex : "#6B7280",
            CategoryIcon = t.Category != null ? t.Category.Icon : "tag",
            Status = t.Status,
            PaymentMethod = t.PaymentMethod,
            Notes = t.Notes,
            CreditCardId = t.CreditCardId,
            CreditCardName = t.CreditCard != null ? t.CreditCard.Name : null,
            InstallmentNumber = t.InstallmentNumber,
            TotalInstallments = t.TotalInstallments,
            InvoiceMonth = t.InvoiceMonth,
            InvoiceYear = t.InvoiceYear,
            IsInvoicePayment = t.IsInvoicePayment,
            RecurrenceGroupId = t.RecurrenceGroupId,
            CreatedAt = t.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> Create([FromBody] TransactionCreateDto dto)
    {
        var userId = GetUserId();

        // Validate category if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories.AnyAsync(c =>
                c.Id == dto.CategoryId.Value && (c.IsDefault || c.UserId == userId));

            if (!categoryExists)
            {
                return BadRequest(new { message = "Categoria inválida ou não pertencente ao usuário." });
            }
        }

        CreditCard? creditCard = null;
        if (dto.PaymentMethod == PaymentMethod.CartaoCredito && dto.CreditCardId.HasValue)
        {
            creditCard = await _context.CreditCards.FirstOrDefaultAsync(c =>
                c.Id == dto.CreditCardId.Value && c.UserId == userId && c.IsActive);

            if (creditCard == null)
            {
                return BadRequest(new { message = "Cartão de crédito inválido ou não encontrado." });
            }
        }

        int totalInstallments = Math.Max(1, dto.Installments ?? 1);

        // If credit card with multiple installments
        if (creditCard != null && totalInstallments > 1)
        {
            decimal perInstallment = Math.Floor((dto.Amount / totalInstallments) * 100m) / 100m;
            decimal remainder = dto.Amount - (perInstallment * totalInstallments);

            var createdTransactions = new List<Transaction>();

            for (int i = 1; i <= totalInstallments; i++)
            {
                decimal instAmount = (i == 1) ? (perInstallment + remainder) : perInstallment;
                var (invYear, invMonth, _) = _cardService.CalculateInvoicePeriod(dto.Date, creditCard.ClosingDay, creditCard.DueDay, installmentOffset: i - 1);

                DateTime instDate = dto.Date;
                if (i > 1)
                {
                    var targetMonthDate = dto.Date.AddMonths(i - 1);
                    int daysInTargetMonth = DateTime.DaysInMonth(targetMonthDate.Year, targetMonthDate.Month);
                    int dayClamped = Math.Min(dto.Date.Day, daysInTargetMonth);
                    instDate = new DateTime(targetMonthDate.Year, targetMonthDate.Month, dayClamped);
                }

                var instTransaction = new Transaction
                {
                    UserId = userId,
                    Date = instDate,
                    Type = dto.Type,
                    Amount = instAmount,
                    Description = $"{dto.Description.Trim()} ({i}/{totalInstallments})",
                    CategoryId = dto.CategoryId,
                    Status = dto.Status,
                    PaymentMethod = PaymentMethod.CartaoCredito,
                    CreditCardId = creditCard.Id,
                    InstallmentNumber = i,
                    TotalInstallments = totalInstallments,
                    InvoiceMonth = invMonth,
                    InvoiceYear = invYear,
                    Notes = dto.Notes?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                createdTransactions.Add(instTransaction);
            }

            _context.Transactions.AddRange(createdTransactions);
            await _context.SaveChangesAsync();

            var first = createdTransactions.First();
            await _context.Entry(first).Reference(t => t.Category).LoadAsync();

            return CreatedAtAction(nameof(GetById), new { id = first.Id }, new TransactionResponseDto
            {
                Id = first.Id,
                Date = first.Date,
                Day = first.Day,
                Month = first.Month,
                Year = first.Year,
                Type = first.Type,
                Amount = first.Amount,
                Description = first.Description,
                CategoryId = first.CategoryId,
                CategoryName = first.Category?.Name ?? "Sem categoria",
                CategoryColor = first.Category?.ColorHex ?? "#6B7280",
                CategoryIcon = first.Category?.Icon ?? "tag",
                Status = first.Status,
                PaymentMethod = first.PaymentMethod,
                Notes = first.Notes,
                CreditCardId = creditCard.Id,
                CreditCardName = creditCard.Name,
                InstallmentNumber = first.InstallmentNumber,
                TotalInstallments = first.TotalInstallments,
                InvoiceMonth = first.InvoiceMonth,
                InvoiceYear = first.InvoiceYear,
                RecurrenceGroupId = first.RecurrenceGroupId,
                CreatedAt = first.CreatedAt
            });
        }

        // Recurring series for non-credit card payment methods (e.g. Salário, Aluguel, Assinaturas)
        if (dto.PaymentMethod != PaymentMethod.CartaoCredito && dto.IsRecurring && dto.RecurrenceMonths.HasValue && dto.RecurrenceMonths.Value > 1)
        {
            int totalMonths = Math.Min(60, Math.Max(2, dto.RecurrenceMonths.Value));
            string recurrenceGroupId = Guid.NewGuid().ToString();

            var recurringTransactions = new List<Transaction>();

            for (int m = 0; m < totalMonths; m++)
            {
                DateTime tDate = dto.Date;
                if (m > 0)
                {
                    var targetMonthDate = dto.Date.AddMonths(m);
                    int daysInTargetMonth = DateTime.DaysInMonth(targetMonthDate.Year, targetMonthDate.Month);
                    int dayClamped = Math.Min(dto.Date.Day, daysInTargetMonth);
                    tDate = new DateTime(targetMonthDate.Year, targetMonthDate.Month, dayClamped);
                }

                // Month 0 keeps the requested status; future months (m > 0) default to Previsto
                var tStatus = (m == 0) ? dto.Status : TransactionStatus.Previsto;

                var recTransaction = new Transaction
                {
                    UserId = userId,
                    Date = tDate,
                    Type = dto.Type,
                    Amount = dto.Amount,
                    Description = dto.Description.Trim(),
                    CategoryId = dto.CategoryId,
                    Status = tStatus,
                    PaymentMethod = dto.PaymentMethod,
                    Notes = dto.Notes?.Trim(),
                    RecurrenceGroupId = recurrenceGroupId,
                    CreatedAt = DateTime.UtcNow
                };

                recurringTransactions.Add(recTransaction);
            }

            _context.Transactions.AddRange(recurringTransactions);
            await _context.SaveChangesAsync();

            var first = recurringTransactions.First();
            if (first.CategoryId.HasValue)
            {
                await _context.Entry(first).Reference(t => t.Category).LoadAsync();
            }

            return CreatedAtAction(nameof(GetById), new { id = first.Id }, new TransactionResponseDto
            {
                Id = first.Id,
                Date = first.Date,
                Day = first.Day,
                Month = first.Month,
                Year = first.Year,
                Type = first.Type,
                Amount = first.Amount,
                Description = first.Description,
                CategoryId = first.CategoryId,
                CategoryName = first.Category?.Name ?? "Sem categoria",
                CategoryColor = first.Category?.ColorHex ?? "#6B7280",
                CategoryIcon = first.Category?.Icon ?? "tag",
                Status = first.Status,
                PaymentMethod = first.PaymentMethod,
                Notes = first.Notes,
                RecurrenceGroupId = first.RecurrenceGroupId,
                CreatedAt = first.CreatedAt
            });
        }

        // Single transaction (1x or other payment methods)
        int? invoiceMonth = null;
        int? invoiceYear = null;
        int? installmentNumber = null;
        int? totalInst = null;

        if (creditCard != null)
        {
            var (invYear, invMonth, _) = _cardService.CalculateInvoicePeriod(dto.Date, creditCard.ClosingDay, creditCard.DueDay, 0);
            invoiceMonth = invMonth;
            invoiceYear = invYear;
            installmentNumber = 1;
            totalInst = 1;
        }

        var transaction = new Transaction
        {
            UserId = userId,
            Date = dto.Date,
            Type = dto.Type,
            Amount = dto.Amount,
            Description = dto.Description.Trim(),
            CategoryId = dto.CategoryId,
            Status = dto.Status,
            PaymentMethod = dto.PaymentMethod,
            CreditCardId = creditCard?.Id,
            InstallmentNumber = installmentNumber,
            TotalInstallments = totalInst,
            InvoiceMonth = invoiceMonth,
            InvoiceYear = invoiceYear,
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Load relations
        await _context.Entry(transaction).Reference(t => t.Category).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, new TransactionResponseDto
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Day = transaction.Day,
            Month = transaction.Month,
            Year = transaction.Year,
            Type = transaction.Type,
            Amount = transaction.Amount,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category != null ? transaction.Category.Name : "Sem categoria",
            CategoryColor = transaction.Category != null ? transaction.Category.ColorHex : "#6B7280",
            CategoryIcon = transaction.Category != null ? transaction.Category.Icon : "tag",
            Status = transaction.Status,
            PaymentMethod = transaction.PaymentMethod,
            Notes = transaction.Notes,
            CreditCardId = creditCard?.Id,
            CreditCardName = creditCard?.Name,
            InstallmentNumber = transaction.InstallmentNumber,
            TotalInstallments = transaction.TotalInstallments,
            InvoiceMonth = transaction.InvoiceMonth,
            InvoiceYear = transaction.InvoiceYear,
            RecurrenceGroupId = transaction.RecurrenceGroupId,
            CreatedAt = transaction.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TransactionResponseDto>> Update(int id, [FromBody] TransactionUpdateDto dto)
    {
        var userId = GetUserId();
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.CreditCard)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound(new { message = "Transação não encontrada." });
        }

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories.AnyAsync(c =>
                c.Id == dto.CategoryId.Value && (c.IsDefault || c.UserId == userId));

            if (!categoryExists)
            {
                return BadRequest(new { message = "Categoria inválida." });
            }
        }

        CreditCard? creditCard = null;
        if (dto.PaymentMethod == PaymentMethod.CartaoCredito && dto.CreditCardId.HasValue)
        {
            creditCard = await _context.CreditCards.FirstOrDefaultAsync(c =>
                c.Id == dto.CreditCardId.Value && c.UserId == userId && c.IsActive);
        }

        transaction.Date = dto.Date;
        transaction.Type = dto.Type;
        transaction.Amount = dto.Amount;
        transaction.Description = dto.Description.Trim();
        transaction.CategoryId = dto.CategoryId;
        transaction.Status = dto.Status;
        transaction.PaymentMethod = dto.PaymentMethod;
        transaction.CreditCardId = creditCard?.Id ?? dto.CreditCardId;
        transaction.Notes = dto.Notes?.Trim();

        if (creditCard != null)
        {
            int offset = Math.Max(0, (transaction.InstallmentNumber ?? 1) - 1);
            var (invYear, invMonth, _) = _cardService.CalculateInvoicePeriod(dto.Date, creditCard.ClosingDay, creditCard.DueDay, offset);
            transaction.InvoiceMonth = invMonth;
            transaction.InvoiceYear = invYear;
        }

        await _context.SaveChangesAsync();
        await _context.Entry(transaction).Reference(t => t.Category).LoadAsync();
        if (transaction.CreditCardId.HasValue)
        {
            await _context.Entry(transaction).Reference(t => t.CreditCard).LoadAsync();
        }

        return Ok(new TransactionResponseDto
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Day = transaction.Day,
            Month = transaction.Month,
            Year = transaction.Year,
            Type = transaction.Type,
            Amount = transaction.Amount,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
            CategoryName = transaction.Category != null ? transaction.Category.Name : "Sem categoria",
            CategoryColor = transaction.Category != null ? transaction.Category.ColorHex : "#6B7280",
            CategoryIcon = transaction.Category != null ? transaction.Category.Icon : "tag",
            Status = transaction.Status,
            PaymentMethod = transaction.PaymentMethod,
            Notes = transaction.Notes,
            CreditCardId = transaction.CreditCardId,
            CreditCardName = transaction.CreditCard?.Name,
            InstallmentNumber = transaction.InstallmentNumber,
            TotalInstallments = transaction.TotalInstallments,
            InvoiceMonth = transaction.InvoiceMonth,
            InvoiceYear = transaction.InvoiceYear,
            IsInvoicePayment = transaction.IsInvoicePayment,
            RecurrenceGroupId = transaction.RecurrenceGroupId,
            CreatedAt = transaction.CreatedAt
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool deleteFuture = false, [FromQuery] bool deleteAllSeries = false)
    {
        var userId = GetUserId();
        var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (transaction == null)
        {
            return NotFound(new { message = "Transação não encontrada." });
        }

        if (!string.IsNullOrEmpty(transaction.RecurrenceGroupId))
        {
            if (deleteAllSeries)
            {
                var series = await _context.Transactions
                    .Where(t => t.UserId == userId && t.RecurrenceGroupId == transaction.RecurrenceGroupId)
                    .ToListAsync();
                _context.Transactions.RemoveRange(series);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else if (deleteFuture)
            {
                var futureSeries = await _context.Transactions
                    .Where(t => t.UserId == userId && t.RecurrenceGroupId == transaction.RecurrenceGroupId && t.Date >= transaction.Date)
                    .ToListAsync();
                _context.Transactions.RemoveRange(futureSeries);
                await _context.SaveChangesAsync();
                return NoContent();
            }
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
