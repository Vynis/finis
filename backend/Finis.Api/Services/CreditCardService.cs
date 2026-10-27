using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Services;

public class CreditCardService : ICreditCardService
{
    private readonly FinisDbContext _context;
    private static readonly string[] MonthNamesPtBr = {
        "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    };

    public CreditCardService(FinisDbContext context)
    {
        _context = context;
    }

    public async Task<List<CreditCardResponseDto>> GetCardsAsync(int userId)
    {
        var cards = await _context.CreditCards
            .Include(c => c.Account)
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var result = new List<CreditCardResponseDto>();

        var paidInvoices = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.IsInvoicePayment && t.CreditCardId != null)
            .Select(t => new { CardId = t.CreditCardId!.Value, Year = t.InvoiceYear ?? t.Date.Year, Month = t.InvoiceMonth ?? t.Date.Month })
            .ToListAsync();

        var paidSet = new HashSet<(int CardId, int Year, int Month)>(
            paidInvoices.Select(p => (p.CardId, p.Year, p.Month))
        );

        var allCardPurchases = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreditCardId != null && !t.IsInvoicePayment && t.PaymentMethod == PaymentMethod.CartaoCredito)
            .Select(t => new { CardId = t.CreditCardId!.Value, t.Amount, t.InvoiceYear, t.InvoiceMonth })
            .ToListAsync();

        var now = DateTime.Today;

        foreach (var card in cards)
        {
            var (currentInvYear, currentInvMonth, _) = CalculateInvoicePeriod(now, card.ClosingDay, card.DueDay);

            // Calculate used limit: all purchases for which the invoice is NOT yet paid
            decimal totalUsed = 0;
            decimal currentInvoiceTotal = 0;

            foreach (var purchase in allCardPurchases.Where(p => p.CardId == card.Id))
            {
                int invYear = purchase.InvoiceYear ?? currentInvYear;
                int invMonth = purchase.InvoiceMonth ?? currentInvMonth;

                bool isPaid = paidSet.Contains((card.Id, invYear, invMonth));
                if (!isPaid)
                {
                    totalUsed += purchase.Amount;
                }

                if (invYear == currentInvYear && invMonth == currentInvMonth)
                {
                    currentInvoiceTotal += purchase.Amount;
                }
            }

            result.Add(new CreditCardResponseDto
            {
                Id = card.Id,
                Name = card.Name,
                Limit = card.Limit,
                ClosingDay = card.ClosingDay,
                DueDay = card.DueDay,
                ColorHex = card.ColorHex,
                Brand = card.Brand,
                AccountId = card.AccountId,
                AccountName = card.Account?.Name,
                IsActive = card.IsActive,
                TotalUsedLimit = totalUsed,
                AvailableLimit = Math.Max(0, card.Limit - totalUsed),
                CurrentInvoiceTotal = currentInvoiceTotal,
                CreatedAt = card.CreatedAt
            });
        }

        return result;
    }

    public async Task<CreditCardResponseDto?> GetCardByIdAsync(int userId, int cardId)
    {
        var cards = await GetCardsAsync(userId);
        return cards.FirstOrDefault(c => c.Id == cardId);
    }

    public async Task<CreditCardResponseDto> CreateCardAsync(int userId, CreditCardCreateUpdateDto dto)
    {
        var card = new CreditCard
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Limit = dto.Limit,
            ClosingDay = dto.ClosingDay,
            DueDay = dto.DueDay,
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#8A05BE" : dto.ColorHex.Trim(),
            Brand = string.IsNullOrWhiteSpace(dto.Brand) ? "Mastercard" : dto.Brand.Trim(),
            AccountId = dto.AccountId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.CreditCards.Add(card);
        await _context.SaveChangesAsync();

        return (await GetCardByIdAsync(userId, card.Id))!;
    }

    public async Task<CreditCardResponseDto?> UpdateCardAsync(int userId, int cardId, CreditCardCreateUpdateDto dto)
    {
        var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card == null) return null;

        card.Name = dto.Name.Trim();
        card.Limit = dto.Limit;
        card.ClosingDay = dto.ClosingDay;
        card.DueDay = dto.DueDay;
        card.ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? "#8A05BE" : dto.ColorHex.Trim();
        card.Brand = string.IsNullOrWhiteSpace(dto.Brand) ? "Mastercard" : dto.Brand.Trim();
        card.AccountId = dto.AccountId;
        card.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return (await GetCardByIdAsync(userId, card.Id))!;
    }

    public async Task<bool> DeleteCardAsync(int userId, int cardId)
    {
        var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card == null) return false;

        card.IsActive = false; // Soft delete to preserve historical data
        await _context.SaveChangesAsync();
        return true;
    }

    public (int Year, int Month, DateTime DueDate) CalculateInvoicePeriod(DateTime purchaseDate, int closingDay, int dueDay, int installmentOffset = 0)
    {
        int daysInPurchaseMonth = DateTime.DaysInMonth(purchaseDate.Year, purchaseDate.Month);
        int actualClosingDay = Math.Min(closingDay, daysInPurchaseMonth);

        DateTime invoiceCycleDate = new DateTime(purchaseDate.Year, purchaseDate.Month, 1);
        if (purchaseDate.Day >= actualClosingDay)
        {
            invoiceCycleDate = invoiceCycleDate.AddMonths(1);
        }

        if (installmentOffset > 0)
        {
            invoiceCycleDate = invoiceCycleDate.AddMonths(installmentOffset);
        }

        int targetYear = invoiceCycleDate.Year;
        int targetMonth = invoiceCycleDate.Month;

        int dueMonth = targetMonth;
        int dueYear = targetYear;
        if (dueDay <= closingDay)
        {
            var nextM = invoiceCycleDate.AddMonths(1);
            dueMonth = nextM.Month;
            dueYear = nextM.Year;
        }

        int daysInDueMonth = DateTime.DaysInMonth(dueYear, dueMonth);
        int actualDueDay = Math.Min(dueDay, daysInDueMonth);
        DateTime dueDate = new DateTime(dueYear, dueMonth, actualDueDay);

        return (dueYear, dueMonth, dueDate);
    }

    public async Task<CreditCardInvoiceSummaryDto?> GetInvoiceSummaryAsync(int userId, int cardId, int year, int month)
    {
        var card = await _context.CreditCards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card == null) return null;

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreditCardId == cardId && !t.IsInvoicePayment
                        && t.InvoiceYear == year && t.InvoiceMonth == month)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var isPaid = await _context.Transactions
            .AsNoTracking()
            .AnyAsync(t => t.UserId == userId && t.CreditCardId == cardId && t.IsInvoicePayment
                           && t.InvoiceYear == year && t.InvoiceMonth == month);

        // Calculate closing and due dates for this invoice
        int dueMonth = month;
        int dueYear = year;
        int cycleMonth = dueMonth;
        int cycleYear = dueYear;
        if (card.DueDay <= card.ClosingDay)
        {
            var prevM = new DateTime(dueYear, dueMonth, 1).AddMonths(-1);
            cycleMonth = prevM.Month;
            cycleYear = prevM.Year;
        }

        int daysInCycleMonth = DateTime.DaysInMonth(cycleYear, cycleMonth);
        int closingDay = Math.Min(card.ClosingDay, daysInCycleMonth);
        DateTime closingDate = new DateTime(cycleYear, cycleMonth, closingDay);

        int daysInDueMonth = DateTime.DaysInMonth(dueYear, dueMonth);
        int dueDay = Math.Min(card.DueDay, daysInDueMonth);
        DateTime dueDate = new DateTime(dueYear, dueMonth, dueDay);

        string status;
        if (isPaid)
        {
            status = "Paga";
        }
        else if (DateTime.Today < closingDate)
        {
            status = "Aberta";
        }
        else
        {
            status = "Fechada";
        }

        var dtoList = transactions.Select(t => new TransactionResponseDto
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
            CategoryName = t.Category?.Name ?? "Sem categoria",
            CategoryColor = t.Category?.ColorHex ?? "#6B7280",
            CategoryIcon = t.Category?.Icon ?? "tag",
            Status = t.Status,
            PaymentMethod = t.PaymentMethod,
            Notes = t.Notes,
            CreditCardId = t.CreditCardId,
            CreditCardName = card.Name,
            InstallmentNumber = t.InstallmentNumber,
            TotalInstallments = t.TotalInstallments,
            InvoiceMonth = t.InvoiceMonth,
            InvoiceYear = t.InvoiceYear,
            IsInvoicePayment = t.IsInvoicePayment,
            CreatedAt = t.CreatedAt
        }).ToList();

        return new CreditCardInvoiceSummaryDto
        {
            CreditCardId = card.Id,
            CreditCardName = card.Name,
            CreditCardColor = card.ColorHex,
            Brand = card.Brand,
            Year = year,
            Month = month,
            MonthName = MonthNamesPtBr[month],
            DueDate = dueDate,
            ClosingDate = closingDate,
            TotalAmount = dtoList.Sum(t => t.Amount),
            IsPaid = isPaid,
            Status = status,
            Transactions = dtoList
        };
    }

    public async Task<List<CreditCardInvoiceSummaryDto>> GetUpcomingInvoicesAsync(int userId, int cardId, int count = 6)
    {
        var card = await _context.CreditCards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card == null) return new List<CreditCardInvoiceSummaryDto>();

        var (currentInvYear, currentInvMonth, _) = CalculateInvoicePeriod(DateTime.Today, card.ClosingDay, card.DueDay);

        var list = new List<CreditCardInvoiceSummaryDto>();
        var baseDate = new DateTime(currentInvYear, currentInvMonth, 1);

        for (int i = 0; i < count; i++)
        {
            var target = baseDate.AddMonths(i);
            var summary = await GetInvoiceSummaryAsync(userId, cardId, target.Year, target.Month);
            if (summary != null)
            {
                list.Add(summary);
            }
        }

        return list;
    }

    public async Task<TransactionResponseDto> PayInvoiceAsync(int userId, int cardId, int year, int month, PayInvoiceRequestDto dto)
    {
        var card = await _context.CreditCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId);
        if (card == null)
        {
            throw new InvalidOperationException("Cartão de crédito não encontrado.");
        }

        // Check if already paid
        var alreadyPaid = await _context.Transactions.AnyAsync(t =>
            t.UserId == userId && t.CreditCardId == cardId && t.IsInvoicePayment &&
            t.InvoiceYear == year && t.InvoiceMonth == month);

        if (alreadyPaid)
        {
            throw new InvalidOperationException("A fatura deste mês já foi registrada como paga.");
        }

        // Get invoice total
        var invoicePurchases = await _context.Transactions
            .Where(t => t.UserId == userId && t.CreditCardId == cardId && !t.IsInvoicePayment &&
                        t.InvoiceYear == year && t.InvoiceMonth == month)
            .ToListAsync();

        decimal amount = dto.Amount ?? invoicePurchases.Sum(t => t.Amount);

        // Get or create "Fatura de Cartão" category
        var category = await _context.Categories.FirstOrDefaultAsync(c =>
            (c.IsDefault || c.UserId == userId) && c.Name == "Fatura de Cartão");

        if (category == null)
        {
            category = new Category
            {
                UserId = userId,
                Name = "Fatura de Cartão",
                Type = TransactionType.SaidaFixa,
                ColorHex = card.ColorHex,
                Icon = "credit-card",
                IsDefault = false
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        // Determine payment date
        int daysInDueMonth = DateTime.DaysInMonth(year, month);
        int dueDay = Math.Min(card.DueDay, daysInDueMonth);
        DateTime defaultDueDate = new DateTime(year, month, dueDay);

        var paymentTransaction = new Transaction
        {
            UserId = userId,
            Date = dto.PaymentDate ?? defaultDueDate,
            Type = TransactionType.SaidaFixa,
            Amount = amount,
            Description = $"Pagamento Fatura {card.Name} - {month:D2}/{year}",
            CategoryId = category.Id,
            Status = TransactionStatus.Realizado,
            PaymentMethod = dto.PaymentMethod,
            CreditCardId = card.Id,
            InvoiceMonth = month,
            InvoiceYear = year,
            IsInvoicePayment = true,
            Notes = $"Fatura quitada contendo {invoicePurchases.Count} itens.",
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(paymentTransaction);
        await _context.SaveChangesAsync();

        await _context.Entry(paymentTransaction).Reference(t => t.Category).LoadAsync();

        return new TransactionResponseDto
        {
            Id = paymentTransaction.Id,
            Date = paymentTransaction.Date,
            Day = paymentTransaction.Day,
            Month = paymentTransaction.Month,
            Year = paymentTransaction.Year,
            Type = paymentTransaction.Type,
            Amount = paymentTransaction.Amount,
            Description = paymentTransaction.Description,
            CategoryId = paymentTransaction.CategoryId,
            CategoryName = category.Name,
            CategoryColor = category.ColorHex,
            CategoryIcon = category.Icon,
            Status = paymentTransaction.Status,
            PaymentMethod = paymentTransaction.PaymentMethod,
            Notes = paymentTransaction.Notes,
            CreditCardId = card.Id,
            CreditCardName = card.Name,
            InvoiceMonth = month,
            InvoiceYear = year,
            IsInvoicePayment = true,
            CreatedAt = paymentTransaction.CreatedAt
        };
    }
}
