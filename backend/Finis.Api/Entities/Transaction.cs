namespace Finis.Api.Entities;

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime Date { get; set; }
    public int Day => Date.Day;
    public int Month => Date.Month;
    public int Year => Date.Year;

    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Realizado;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;
    public string? Notes { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Multi-Account Integration (Pessoal vs Empresa)
    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    // Credit Card Integration
    public int? CreditCardId { get; set; }
    public CreditCard? CreditCard { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? TotalInstallments { get; set; }
    public int? InvoiceMonth { get; set; }
    public int? InvoiceYear { get; set; }
    public bool IsInvoicePayment { get; set; } = false;

    // Recurring / Fixed Monthly Replication
    public string? RecurrenceGroupId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
