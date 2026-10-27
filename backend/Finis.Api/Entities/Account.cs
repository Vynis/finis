namespace Finis.Api.Entities;

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; } = AccountType.Pessoal;
    public decimal InitialBalance { get; set; } = 0.00m;
    public string ColorHex { get; set; } = "#10B981";
    public string Icon { get; set; } = "wallet2";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
}
