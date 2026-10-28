namespace Finis.Api.Entities;

public class Debt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Creditor { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal InterestRateMonthly { get; set; } // % a.m. (e.g. 2.50 = 2.5% a.m.)
    public decimal MonthlyPayment { get; set; }
    public int DueDay { get; set; } = 10;
    public string ColorHex { get; set; } = "#EF4444";
    public string? Notes { get; set; }
    public bool IsSettled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
