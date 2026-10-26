using System.ComponentModel.DataAnnotations;
using Finis.Api.Entities;

namespace Finis.Api.DTOs;

public class CreditCardCreateUpdateDto
{
    [Required(ErrorMessage = "O nome do cartão é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O limite é obrigatório.")]
    [Range(0.01, 999999999.99, ErrorMessage = "O limite deve ser maior que zero.")]
    public decimal Limit { get; set; }

    [Required(ErrorMessage = "O dia de fechamento é obrigatório.")]
    [Range(1, 31, ErrorMessage = "O dia de fechamento deve ser entre 1 e 31.")]
    public int ClosingDay { get; set; }

    [Required(ErrorMessage = "O dia de vencimento é obrigatório.")]
    [Range(1, 31, ErrorMessage = "O dia de vencimento deve ser entre 1 e 31.")]
    public int DueDay { get; set; }

    [StringLength(20)]
    public string ColorHex { get; set; } = "#8A05BE";

    [StringLength(50)]
    public string Brand { get; set; } = "Mastercard";

    public bool IsActive { get; set; } = true;
}

public class CreditCardResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public string ColorHex { get; set; } = "#8A05BE";
    public string Brand { get; set; } = "Mastercard";
    public bool IsActive { get; set; }
    public decimal TotalUsedLimit { get; set; }
    public decimal AvailableLimit { get; set; }
    public decimal CurrentInvoiceTotal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreditCardInvoiceSummaryDto
{
    public int CreditCardId { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
    public string CreditCardColor { get; set; } = "#8A05BE";
    public string Brand { get; set; } = "Mastercard";
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime ClosingDate { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public string Status { get; set; } = "Aberta"; // "Aberta", "Fechada", "Paga"
    public List<TransactionResponseDto> Transactions { get; set; } = new();
}

public class PayInvoiceRequestDto
{
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;
    public DateTime? PaymentDate { get; set; }
    public decimal? Amount { get; set; }
}
