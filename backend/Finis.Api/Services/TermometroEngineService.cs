using System.Globalization;
using Finis.Api.Data;
using Finis.Api.DTOs;
using Finis.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Services;

public class TermometroEngineService : ITermometroEngineService
{
    private readonly FinisDbContext _context;
    private static readonly string[] MonthNamesPtBr = {
        "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    };

    private static readonly string[] DayOfWeekNamesPtBr = {
        "Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"
    };

    public TermometroEngineService(FinisDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetCarryOverBalanceAsync(int userId, DateTime targetMonthStart)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return 0;

        decimal initialBalance = user.InitialBalance;

        // Sum all income transactions before targetMonthStart
        var pastIncome = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date < targetMonthStart && t.Type == TransactionType.Entrada)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Only transactions that actually took money out of the bank account before targetMonthStart:
        // That is: cash/pix/debito expenses OR invoice payment transactions.
        var pastExpenses = await _context.Transactions
            .Where(t => t.UserId == userId && t.Date < targetMonthStart &&
                        (t.Type == TransactionType.SaidaFixa || t.Type == TransactionType.Diario) &&
                        (t.PaymentMethod != PaymentMethod.CartaoCredito || t.IsInvoicePayment))
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        return initialBalance + pastIncome - pastExpenses;
    }

    public async Task<MonthlyFlowResponseDto> GetMonthlyFlowAsync(int userId, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthEnd = new DateTime(year, month, daysInMonth, 23, 59, 59);

        // Carry-over balance from month start
        decimal runningBalance = await GetCarryOverBalanceAsync(userId, monthStart);
        decimal monthInitialBalance = runningBalance;

        // Fetch transactions for this month
        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId && t.Date >= monthStart && t.Date <= monthEnd)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToListAsync();

        // Active credit cards
        var activeCards = await _context.CreditCards
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync();

        // Invoices maturing in this month
        var cardInvoicesForMonth = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreditCardId != null && !t.IsInvoicePayment
                        && t.InvoiceYear == year && t.InvoiceMonth == month)
            .GroupBy(t => t.CreditCardId!.Value)
            .Select(g => new { CardId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        var paidCardInvoices = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreditCardId != null && t.IsInvoicePayment
                        && t.InvoiceYear == year && t.InvoiceMonth == month)
            .Select(t => t.CreditCardId!.Value)
            .ToListAsync();

        var paidCardSet = new HashSet<int>(paidCardInvoices);

        // Build list of projected unpaid invoices
        var projectedInvoices = new List<TransactionResponseDto>();
        foreach (var card in activeCards)
        {
            var invoiceData = cardInvoicesForMonth.FirstOrDefault(i => i.CardId == card.Id);
            decimal invTotal = invoiceData?.Total ?? 0;

            if (invTotal > 0 && !paidCardSet.Contains(card.Id))
            {
                int dueDayClamped = Math.Min(card.DueDay, daysInMonth);
                projectedInvoices.Add(new TransactionResponseDto
                {
                    Id = -card.Id, // Negative ID signals projected item
                    Date = new DateTime(year, month, dueDayClamped),
                    Day = dueDayClamped,
                    Month = month,
                    Year = year,
                    Type = TransactionType.SaidaFixa,
                    Amount = invTotal,
                    Description = $"Fatura {card.Name} (Previsto)",
                    CategoryName = "Fatura de Cartão",
                    CategoryColor = card.ColorHex,
                    CategoryIcon = "credit-card",
                    Status = TransactionStatus.Previsto,
                    PaymentMethod = PaymentMethod.CartaoCredito,
                    CreditCardId = card.Id,
                    CreditCardName = card.Name,
                    Notes = $"Fatura do cartão prevista para o dia {dueDayClamped}. Clique em Pagar Fatura para efetivar.",
                    IsInvoicePayment = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var daysList = new List<DailyFlowDto>();
        decimal monthTotalIncome = 0;
        decimal monthTotalFixed = 0;
        decimal monthTotalDaily = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day);

            var dayActualTransactions = transactions
                .Where(t => t.Date.Day == day)
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
                    CategoryName = t.Category?.Name ?? "Sem categoria",
                    CategoryColor = t.Category?.ColorHex ?? "#6B7280",
                    CategoryIcon = t.Category?.Icon ?? "tag",
                    Status = t.Status,
                    PaymentMethod = t.PaymentMethod,
                    Notes = t.Notes,
                    CreditCardId = t.CreditCardId,
                    CreditCardName = t.CreditCard?.Name,
                    InstallmentNumber = t.InstallmentNumber,
                    TotalInstallments = t.TotalInstallments,
                    InvoiceMonth = t.InvoiceMonth,
                    InvoiceYear = t.InvoiceYear,
                    IsInvoicePayment = t.IsInvoicePayment,
                    CreatedAt = t.CreatedAt
                })
                .ToList();

            var dayProjectedInvoices = projectedInvoices.Where(p => p.Day == day).ToList();

            // All transactions to show on this day
            var allDayTransactions = dayActualTransactions.Concat(dayProjectedInvoices).ToList();

            decimal dayIncome = dayActualTransactions.Where(t => t.Type == TransactionType.Entrada).Sum(t => t.Amount);

            // Fixed expenses from account:
            // Non-card fixed expenses OR actual invoice payment transactions
            decimal dayFixedActual = dayActualTransactions
                .Where(t => t.Type == TransactionType.SaidaFixa && (t.PaymentMethod != PaymentMethod.CartaoCredito || t.IsInvoicePayment))
                .Sum(t => t.Amount);

            // Plus any projected invoices maturing today
            decimal dayFixedProjected = dayProjectedInvoices.Sum(p => p.Amount);
            decimal dayFixed = dayFixedActual + dayFixedProjected;

            // Daily expenses from account (non-card)
            decimal dayDaily = dayActualTransactions
                .Where(t => t.Type == TransactionType.Diario && t.PaymentMethod != PaymentMethod.CartaoCredito)
                .Sum(t => t.Amount);

            // Cascade formula: Saldo(d) = Saldo(d-1) + Entrada(d) - (SaidaFixa(d) + Diario(d))
            runningBalance = runningBalance + dayIncome - (dayFixed + dayDaily);

            monthTotalIncome += dayIncome;
            monthTotalFixed += dayFixed;
            monthTotalDaily += dayDaily;

            daysList.Add(new DailyFlowDto
            {
                Day = day,
                Date = currentDate,
                DayOfWeek = DayOfWeekNamesPtBr[(int)currentDate.DayOfWeek],
                TotalIncome = dayIncome,
                TotalFixedExpenses = dayFixed,
                TotalDailyExpenses = dayDaily,
                CumulativeBalance = runningBalance,
                TransactionsCount = allDayTransactions.Count,
                Transactions = allDayTransactions
            });
        }

        decimal monthTotalExpenses = monthTotalFixed + monthTotalDaily;
        decimal performance = monthTotalIncome - monthTotalExpenses;
        decimal savingsRate = monthTotalIncome > 0
            ? Math.Round((performance / monthTotalIncome) * 100m, 2)
            : 0;

        return new MonthlyFlowResponseDto
        {
            Year = year,
            Month = month,
            MonthName = MonthNamesPtBr[month],
            InitialBalance = monthInitialBalance,
            TotalIncome = monthTotalIncome,
            TotalFixedExpenses = monthTotalFixed,
            TotalDailyExpenses = monthTotalDaily,
            TotalExpenses = monthTotalExpenses,
            Performance = performance,
            FinalBalance = runningBalance,
            SavingsRate = savingsRate,
            Days = daysList
        };
    }

    public async Task<AnnualEconomyResponseDto> GetAnnualEconomyAsync(int userId, int year)
    {
        var yearStart = new DateTime(year, 1, 1);
        var initialBalanceOfYear = await GetCarryOverBalanceAsync(userId, yearStart);

        var monthsSummary = new List<AnnualMonthSummaryDto>();
        decimal yearTotalIncome = 0;
        decimal yearTotalExpenses = 0;

        for (int m = 1; m <= 12; m++)
        {
            var monthlyFlow = await GetMonthlyFlowAsync(userId, year, m);

            yearTotalIncome += monthlyFlow.TotalIncome;
            yearTotalExpenses += monthlyFlow.TotalExpenses;

            monthsSummary.Add(new AnnualMonthSummaryDto
            {
                Month = m,
                MonthName = MonthNamesPtBr[m],
                TotalIncome = monthlyFlow.TotalIncome,
                TotalExpenses = monthlyFlow.TotalExpenses,
                Performance = monthlyFlow.Performance,
                SavingsRate = monthlyFlow.SavingsRate,
                EndOfMonthBalance = monthlyFlow.FinalBalance
            });
        }

        decimal yearTotalPerformance = yearTotalIncome - yearTotalExpenses;
        decimal overallSavingsRate = yearTotalIncome > 0
            ? Math.Round((yearTotalPerformance / yearTotalIncome) * 100m, 2)
            : 0;

        decimal endOfYearBalance = monthsSummary.LastOrDefault()?.EndOfMonthBalance ?? initialBalanceOfYear;

        return new AnnualEconomyResponseDto
        {
            Year = year,
            InitialBalanceOfYear = initialBalanceOfYear,
            TotalIncome = yearTotalIncome,
            TotalExpenses = yearTotalExpenses,
            TotalPerformance = yearTotalPerformance,
            OverallSavingsRate = overallSavingsRate,
            EndOfYearBalance = endOfYearBalance,
            Months = monthsSummary
        };
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, int year, int month)
    {
        var monthlyFlow = await GetMonthlyFlowAsync(userId, year, month);

        // Recent 5 transactions
        var recent = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.CreditCard)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Take(5)
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
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        // Daily trend for chart
        var trend = monthlyFlow.Days.Select(d => new DailyTrendPointDto
        {
            Day = d.Day,
            Label = $"Dia {d.Day}",
            Balance = d.CumulativeBalance,
            Income = d.TotalIncome,
            Expense = d.TotalExpenses
        }).ToList();

        // Expense category distribution (includes credit card purchases to reflect real spending per category)
        var monthStart = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthEnd = new DateTime(year, month, daysInMonth, 23, 59, 59);

        var expensesByCategory = await _context.Transactions
            .Include(t => t.Category)
            .Where(t => t.UserId == userId && t.Date >= monthStart && t.Date <= monthEnd &&
                        (t.Type == TransactionType.SaidaFixa || t.Type == TransactionType.Diario))
            .GroupBy(t => new { Name = t.Category != null ? t.Category.Name : "Sem Categoria", Color = t.Category != null ? t.Category.ColorHex : "#6B7280" })
            .Select(g => new
            {
                CategoryName = g.Key.Name,
                ColorHex = g.Key.Color,
                Total = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        decimal totalExpensesCategory = expensesByCategory.Sum(x => x.Total);
        var categoryShares = expensesByCategory.Select(c => new CategoryExpenseShareDto
        {
            CategoryName = c.CategoryName,
            ColorHex = c.ColorHex,
            TotalAmount = c.Total,
            Percentage = totalExpensesCategory > 0 ? (double)Math.Round((c.Total / totalExpensesCategory) * 100m, 1) : 0
        }).OrderByDescending(c => c.TotalAmount).ToList();

        // Total credit card invoices for this month
        var cardInvoicesSum = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.CreditCardId != null && !t.IsInvoicePayment
                        && t.InvoiceYear == year && t.InvoiceMonth == month)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Current real-time balance
        var today = DateTime.UtcNow;
        var todayFlow = monthlyFlow.Days.FirstOrDefault(d => d.Day == today.Day);
        decimal currentBalance = (year == today.Year && month == today.Month && todayFlow != null)
            ? todayFlow.CumulativeBalance
            : monthlyFlow.FinalBalance;

        return new DashboardSummaryDto
        {
            Year = year,
            Month = month,
            MonthName = monthlyFlow.MonthName,
            CurrentBalance = currentBalance,
            InitialBalance = monthlyFlow.InitialBalance,
            MonthIncome = monthlyFlow.TotalIncome,
            MonthFixedExpenses = monthlyFlow.TotalFixedExpenses,
            MonthDailyExpenses = monthlyFlow.TotalDailyExpenses,
            MonthTotalExpenses = monthlyFlow.TotalExpenses,
            MonthCreditCardTotal = cardInvoicesSum,
            MonthPerformance = monthlyFlow.Performance,
            MonthSavingsRate = monthlyFlow.SavingsRate,
            RecentTransactions = recent,
            DailyTrend = trend,
            CategoryDistribution = categoryShares
        };
    }
}
