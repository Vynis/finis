using System.ComponentModel.DataAnnotations;

namespace Finis.Api.DTOs;

public class GoalCreateUpdateDto
{
    [Required]
    [StringLength(100, ErrorMessage = "O nome da meta deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public int? AccountId { get; set; }

    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "O valor alvo deve ser maior que zero.")]
    public decimal TargetAmount { get; set; }

    [Range(0.00, 999999999.99)]
    public decimal CurrentAmount { get; set; } = 0.00m;

    public DateTime? TargetDate { get; set; }

    public string Category { get; set; } = "Geral";
    public string ColorHex { get; set; } = "#10B981";
    public string Icon { get; set; } = "bullseye";
    public int Priority { get; set; } = 2; // 1 = Alta, 2 = Média, 3 = Baixa
    public bool IsCompleted { get; set; } = false;
}

public class GoalResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);
    public double PercentageCompleted => TargetAmount > 0
        ? Math.Min(100.0, Math.Round((double)(CurrentAmount / TargetAmount) * 100.0, 1))
        : 0;

    public DateTime? TargetDate { get; set; }
    public string Category { get; set; } = "Geral";
    public string ColorHex { get; set; } = "#10B981";
    public string Icon { get; set; } = "bullseye";
    public int Priority { get; set; } = 2;
    public string PriorityLabel => Priority switch
    {
        1 => "Alta",
        3 => "Baixa",
        _ => "Média"
    };

    public bool IsCompleted { get; set; }
    public decimal MonthlyRequiredSavings { get; set; }
    public int? MonthsRemaining { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GoalContributionDto
{
    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    public bool IsWithdrawal { get; set; } = false;
}

public class DebtCreateUpdateDto
{
    [Required]
    [StringLength(100, ErrorMessage = "O nome da dívida deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    public string? Creditor { get; set; }
    public int? AccountId { get; set; }

    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "O valor total deve ser maior que zero.")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Range(0.00, 999999999.99)]
    public decimal RemainingAmount { get; set; }

    [Range(0.00, 100.00, ErrorMessage = "A taxa de juros deve estar entre 0% e 100% ao mês.")]
    public decimal InterestRateMonthly { get; set; } = 0.00m;

    [Range(0.00, 999999999.99)]
    public decimal MonthlyPayment { get; set; } = 0.00m;

    [Range(1, 31)]
    public int DueDay { get; set; } = 10;

    public string ColorHex { get; set; } = "#EF4444";
    public string? Notes { get; set; }
    public bool IsSettled { get; set; } = false;
}

public class DebtResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Creditor { get; set; }
    public int? AccountId { get; set; }
    public string? AccountName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal PaidAmount => Math.Max(0, TotalAmount - RemainingAmount);
    public double PercentagePaid => TotalAmount > 0
        ? Math.Min(100.0, Math.Round((double)(PaidAmount / TotalAmount) * 100.0, 1))
        : 0;

    public decimal InterestRateMonthly { get; set; }
    public decimal MonthlyPayment { get; set; }
    public int DueDay { get; set; }
    public string ColorHex { get; set; } = "#EF4444";
    public string? Notes { get; set; }
    public bool IsSettled { get; set; }
    public bool IsHighInterest => InterestRateMonthly >= 3.0m;
    public int? EstimatedMonthsToPayoff { get; set; }
    public decimal MonthlyInterestCostEstimate => RemainingAmount * (InterestRateMonthly / 100m);
    public DateTime CreatedAt { get; set; }
}

public class DebtAmortizeDto
{
    [Required]
    [Range(0.01, 999999999.99, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }
}

public class AdvisorAnalysisDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;

    // Termômetro / Cashflow metrics
    public decimal ProjectedSurplus { get; set; } // Sobra projetada do mês (Performance)
    public decimal CurrentRealizedSurplus { get; set; } // Sobra realizada até hoje
    public decimal DailySafeSpendingTarget { get; set; } // Teto diário seguro
    public int DaysRemainingInMonth { get; set; }

    // Health Score
    public string HealthStatus { get; set; } = "Equilibrado"; // Excelente, Equilibrado, Atenção, Crítico
    public string HealthBadgeClass { get; set; } = "bg-success";
    public string PrimaryRecommendationTitle { get; set; } = string.Empty;
    public string PrimaryRecommendationMessage { get; set; } = string.Empty;

    // Totals overview
    public decimal TotalDebtsRemaining { get; set; }
    public decimal TotalMonthlyInterestCost { get; set; }
    public decimal TotalGoalsTarget { get; set; }
    public decimal TotalGoalsCurrent { get; set; }

    // Action Plan
    public List<DebtRecommendationDto> DebtActions { get; set; } = new();
    public List<GoalRecommendationDto> GoalActions { get; set; } = new();
    public List<string> SmartTips { get; set; } = new();
}

public class DebtRecommendationDto
{
    public int DebtId { get; set; }
    public string DebtName { get; set; } = string.Empty;
    public string? Creditor { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal InterestRateMonthly { get; set; }
    public decimal SuggestedAmortization { get; set; }
    public decimal EstimatedInterestSavedMonthly { get; set; }
    public string UrgencyLabel { get; set; } = "Alta Prioridade";
    public string StrategyReason { get; set; } = string.Empty;
}

public class GoalRecommendationDto
{
    public int GoalId { get; set; }
    public string GoalName { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal SuggestedDeposit { get; set; }
    public int? MonthsAhead { get; set; }
    public string StrategyReason { get; set; } = string.Empty;
}
