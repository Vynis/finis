using Finis.Api.Entities;

namespace Finis.Api.DTOs;

public class DailyFlowDto
{
    public int Day { get; set; }
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalFixedExpenses { get; set; }
    public decimal TotalDailyExpenses { get; set; }
    public decimal TotalExpenses => TotalFixedExpenses + TotalDailyExpenses;
    public decimal NetDay => TotalIncome - TotalExpenses;
    public decimal CumulativeBalance { get; set; }
    public int TransactionsCount { get; set; }
    public List<TransactionResponseDto> Transactions { get; set; } = new();
}

public class MonthlyFlowResponseDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalFixedExpenses { get; set; }
    public decimal TotalDailyExpenses { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Performance { get; set; }
    public decimal FinalBalance { get; set; }
    public decimal SavingsRate { get; set; }
    public List<DailyFlowDto> Days { get; set; } = new();
}

public class AnnualMonthSummaryDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Performance { get; set; }
    public decimal SavingsRate { get; set; }
    public decimal EndOfMonthBalance { get; set; }
}

public class AnnualEconomyResponseDto
{
    public int Year { get; set; }
    public decimal InitialBalanceOfYear { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalPerformance { get; set; }
    public decimal OverallSavingsRate { get; set; }
    public decimal EndOfYearBalance { get; set; }
    public List<AnnualMonthSummaryDto> Months { get; set; } = new();
}

public class DashboardSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal MonthIncome { get; set; }
    public decimal MonthFixedExpenses { get; set; }
    public decimal MonthDailyExpenses { get; set; }
    public decimal MonthTotalExpenses { get; set; }
    public decimal MonthCreditCardTotal { get; set; }
    public decimal MonthPerformance { get; set; }
    public decimal MonthSavingsRate { get; set; }

    // Previsto x Realizado metrics
    public decimal RealizedIncome { get; set; }
    public decimal PendingIncome { get; set; }
    public decimal RealizedExpenses { get; set; }
    public decimal PendingExpenses { get; set; }
    public decimal RealizedPerformance { get; set; }
    public decimal PendingPerformance { get; set; }

    // Overdue summary
    public OverdueSummaryDto OverdueSummary { get; set; } = new();

    public List<TransactionResponseDto> RecentTransactions { get; set; } = new();
    public List<DailyTrendPointDto> DailyTrend { get; set; } = new();
    public List<CategoryExpenseShareDto> CategoryDistribution { get; set; } = new();
}

public class DailyTrendPointDto
{
    public int Day { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class CategoryExpenseShareDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#3B82F6";
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string TypeName => Type.ToString();
    public string ColorHex { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "tag";
    public bool IsDefault { get; set; }
}

public class OverdueSummaryDto
{
    public int TotalCount { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalIncome { get; set; }
    public List<TransactionResponseDto> Transactions { get; set; } = new();
}
