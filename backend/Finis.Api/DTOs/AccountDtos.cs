using System.ComponentModel.DataAnnotations;
using Finis.Api.Entities;

namespace Finis.Api.DTOs;

public class AccountCreateDto
{
    [Required]
    [StringLength(100, ErrorMessage = "O nome da conta deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; } = AccountType.Pessoal;

    public decimal InitialBalance { get; set; } = 0.00m;

    [StringLength(20)]
    public string ColorHex { get; set; } = "#10B981";

    [StringLength(50)]
    public string Icon { get; set; } = "wallet2";
}

public class AccountUpdateDto
{
    [Required]
    [StringLength(100, ErrorMessage = "O nome da conta deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; } = AccountType.Pessoal;

    public decimal InitialBalance { get; set; } = 0.00m;

    [StringLength(20)]
    public string ColorHex { get; set; } = "#10B981";

    [StringLength(50)]
    public string Icon { get; set; } = "wallet2";

    public bool IsActive { get; set; } = true;
}

public class AccountResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string TypeName => Type.ToString();
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string ColorHex { get; set; } = "#10B981";
    public string Icon { get; set; } = "wallet2";
    public bool IsActive { get; set; }
    public int TransactionsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
