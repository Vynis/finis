using System.ComponentModel.DataAnnotations;
using Finis.Api.Entities;

namespace Finis.Api.DTOs;

public class TransactionCreateDto
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(200, ErrorMessage = "A descrição deve ter até 200 caracteres.")]
    public string Description { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Realizado;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;
    public string? Notes { get; set; }

    // Credit card options
    public int? CreditCardId { get; set; }
    [Range(1, 60, ErrorMessage = "O número de parcelas deve ser entre 1 e 60.")]
    public int? Installments { get; set; } = 1;
}

public class TransactionUpdateDto
{
    [Required]
    public DateTime Date { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(200, ErrorMessage = "A descrição deve ter até 200 caracteres.")]
    public string Description { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Realizado;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;
    public string? Notes { get; set; }

    public int? CreditCardId { get; set; }
}

public class TransactionResponseDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int Day { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public TransactionType Type { get; set; }
    public string TypeName => Type.ToString();
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = "Sem categoria";
    public string CategoryColor { get; set; } = "#6B7280";
    public string CategoryIcon { get; set; } = "tag";
    public TransactionStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public string? Notes { get; set; }

    // Credit Card info
    public int? CreditCardId { get; set; }
    public string? CreditCardName { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? TotalInstallments { get; set; }
    public int? InvoiceMonth { get; set; }
    public int? InvoiceYear { get; set; }
    public bool IsInvoicePayment { get; set; }

    public DateTime CreatedAt { get; set; }
}
