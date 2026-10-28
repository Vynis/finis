namespace Finis.Api.Entities;

public class Goal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Category { get; set; } = "Geral";
    public string ColorHex { get; set; } = "#10B981";
    public string Icon { get; set; } = "bullseye";
    public int Priority { get; set; } = 2; // 1 = Alta, 2 = Média, 3 = Baixa
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
