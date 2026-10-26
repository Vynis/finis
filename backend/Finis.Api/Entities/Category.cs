namespace Finis.Api.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string ColorHex { get; set; } = "#3B82F6";
    public string Icon { get; set; } = "tag";
    public bool IsDefault { get; set; } = false;

    public int? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
