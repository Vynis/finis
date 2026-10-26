namespace Finis.Api.Entities;

public class CreditCard
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; } // 1..31
    public int DueDay { get; set; }     // 1..31
    public string ColorHex { get; set; } = "#8A05BE";
    public string Brand { get; set; } = "Mastercard";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
