namespace Finis.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; } = 0.00m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<Debt> Debts { get; set; } = new List<Debt>();
}
