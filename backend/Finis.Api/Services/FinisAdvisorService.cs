using Finis.Api.Data;
using Finis.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Finis.Api.Services;

public class FinisAdvisorService : IFinisAdvisorService
{
    private readonly FinisDbContext _context;
    private readonly ITermometroEngineService _engineService;

    public FinisAdvisorService(FinisDbContext context, ITermometroEngineService engineService)
    {
        _context = context;
        _engineService = engineService;
    }

    public async Task<AdvisorAnalysisDto> GetAdvisorAnalysisAsync(int userId, int year, int month, int? accountId = null)
    {
        var monthlyFlow = await _engineService.GetMonthlyFlowAsync(userId, year, month, accountId);

        var today = DateTime.Today;
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int currentDay = (year == today.Year && month == today.Month) ? today.Day : daysInMonth;
        int daysRemaining = Math.Max(1, daysInMonth - currentDay + 1);

        // Fetch active debts
        var debtsQuery = _context.Debts
            .Include(d => d.Account)
            .Where(d => d.UserId == userId && !d.IsSettled);

        if (accountId.HasValue)
        {
            debtsQuery = debtsQuery.Where(d => d.AccountId == accountId.Value);
        }

        var debts = await debtsQuery
            .OrderByDescending(d => d.InterestRateMonthly)
            .ThenByDescending(d => d.RemainingAmount)
            .ToListAsync();

        // Fetch active goals
        var goalsQuery = _context.Goals
            .Include(g => g.Account)
            .Where(g => g.UserId == userId && !g.IsCompleted);

        if (accountId.HasValue)
        {
            goalsQuery = goalsQuery.Where(g => g.AccountId == accountId.Value);
        }

        var goals = await goalsQuery
            .OrderBy(g => g.Priority)
            .ThenBy(g => g.TargetDate)
            .ToListAsync();

        decimal projectedSurplus = monthlyFlow.Performance; // TotalIncome - TotalExpenses
        decimal currentBalance = monthlyFlow.Days.FirstOrDefault(d => d.Day == currentDay)?.CumulativeBalance ?? monthlyFlow.FinalBalance;

        // Daily safe spending target
        decimal remainingDailyBudget = monthlyFlow.TotalIncome - monthlyFlow.TotalFixedExpenses;
        decimal safeDailyTarget = remainingDailyBudget > 0 ? Math.Round(remainingDailyBudget / daysInMonth, 2) : 0;

        decimal totalDebtsRemaining = debts.Sum(d => d.RemainingAmount);
        decimal totalMonthlyInterestCost = debts.Sum(d => d.RemainingAmount * (d.InterestRateMonthly / 100m));
        decimal totalGoalsTarget = goals.Sum(g => g.TargetAmount);
        decimal totalGoalsCurrent = goals.Sum(g => g.CurrentAmount);

        var debtActions = new List<DebtRecommendationDto>();
        var goalActions = new List<GoalRecommendationDto>();
        var tips = new List<string>();

        string healthStatus = "Equilibrado";
        string healthBadgeClass = "bg-success";
        string recTitle = "";
        string recMessage = "";

        // Available surplus for advisor strategy (use projected surplus if positive, clamped)
        decimal investableSurplus = Math.Max(0, projectedSurplus);

        // 1. DEBT STRATEGY: AVALANCHE (HIGHEST INTEREST) OR SNOWBALL (SMALLEST BALANCE)
        var expensiveDebts = debts.Where(d => d.InterestRateMonthly > 0)
            .OrderByDescending(d => d.InterestRateMonthly)
            .ThenByDescending(d => d.RemainingAmount)
            .ToList();

        var zeroInterestDebts = debts.Where(d => d.InterestRateMonthly == 0)
            .OrderBy(d => d.RemainingAmount) // Snowball: menor saldo primeiro
            .ToList();

        if (expensiveDebts.Any())
        {
            // Highest interest debt is the primary priority (Método Avalanche)
            var topDebt = expensiveDebts.First();
            decimal suggestedDebtAmortization = 0;

            if (investableSurplus > 0)
            {
                // Allocate up to 75% of surplus to highest interest debt, capped at remaining amount
                suggestedDebtAmortization = Math.Min(topDebt.RemainingAmount, Math.Round(investableSurplus * 0.75m, 2));
            }

            debtActions.Add(new DebtRecommendationDto
            {
                DebtId = topDebt.Id,
                DebtName = topDebt.Name,
                Creditor = topDebt.Creditor,
                RemainingAmount = topDebt.RemainingAmount,
                InterestRateMonthly = topDebt.InterestRateMonthly,
                SuggestedAmortization = suggestedDebtAmortization,
                EstimatedInterestSavedMonthly = suggestedDebtAmortization * (topDebt.InterestRateMonthly / 100m),
                UrgencyLabel = topDebt.InterestRateMonthly >= 3.0m ? "Prioridade Máxima (Juros Altos)" : "Recomendado Amortizar",
                StrategyReason = $"Pelo Método Avalanche, amortizar esta dívida que cobra {topDebt.InterestRateMonthly}% a.m. rende mais do que qualquer investimento de renda fixa."
            });

            // Other debts with interest
            foreach (var d in expensiveDebts.Skip(1))
            {
                debtActions.Add(new DebtRecommendationDto
                {
                    DebtId = d.Id,
                    DebtName = d.Name,
                    Creditor = d.Creditor,
                    RemainingAmount = d.RemainingAmount,
                    InterestRateMonthly = d.InterestRateMonthly,
                    SuggestedAmortization = 0,
                    EstimatedInterestSavedMonthly = 0,
                    UrgencyLabel = "Manter Pagamento Mínimo",
                    StrategyReason = "Pague apenas a parcela programada enquanto concentramos a sobra na dívida com a maior taxa de juros."
                });
            }

            // Also include zero interest debts so user sees all active debts
            foreach (var d in zeroInterestDebts)
            {
                debtActions.Add(new DebtRecommendationDto
                {
                    DebtId = d.Id,
                    DebtName = d.Name,
                    Creditor = d.Creditor,
                    RemainingAmount = d.RemainingAmount,
                    InterestRateMonthly = 0,
                    SuggestedAmortization = 0,
                    EstimatedInterestSavedMonthly = 0,
                    UrgencyLabel = "Sem Juros (Parcelamento)",
                    StrategyReason = "Sem juros rotativos ativos. Mantenha as parcelas em dia enquanto eliminamos as dívidas com juros."
                });
            }
        }
        else if (zeroInterestDebts.Any())
        {
            // All debts are 0% interest (e.g. credit cards with no revolving interest / interest-free installments)
            // Use SNOWBALL METHOD: prioritize the debt with the smallest balance to eliminate whole accounts faster
            var smallestDebt = zeroInterestDebts.First();
            decimal suggestedDebtAmortization = 0;

            if (investableSurplus > 0)
            {
                // Allocate up to 50% of surplus to speed up eliminating the smallest debt, leaving room for goals/emergency fund
                suggestedDebtAmortization = Math.Min(smallestDebt.RemainingAmount, Math.Round(investableSurplus * 0.50m, 2));
            }

            debtActions.Add(new DebtRecommendationDto
            {
                DebtId = smallestDebt.Id,
                DebtName = smallestDebt.Name,
                Creditor = smallestDebt.Creditor,
                RemainingAmount = smallestDebt.RemainingAmount,
                InterestRateMonthly = 0,
                SuggestedAmortization = suggestedDebtAmortization,
                EstimatedInterestSavedMonthly = 0,
                UrgencyLabel = "Método Bola de Neve (Menor Saldo)",
                StrategyReason = $"Pelo Método Bola de Neve, quitar primeiro a dívida de menor saldo (R$ {smallestDebt.RemainingAmount:N2}) elimina um cartão/credor mais rápido e simplifica sua gestão mensal."
            });

            foreach (var d in zeroInterestDebts.Skip(1))
            {
                debtActions.Add(new DebtRecommendationDto
                {
                    DebtId = d.Id,
                    DebtName = d.Name,
                    Creditor = d.Creditor,
                    RemainingAmount = d.RemainingAmount,
                    InterestRateMonthly = 0,
                    SuggestedAmortization = 0,
                    EstimatedInterestSavedMonthly = 0,
                    UrgencyLabel = "Manter Parcelas em Dia",
                    StrategyReason = "Sem cobrança de juros ativos. Mantenha os pagamentos regulares em dia."
                });
            }
        }

        // 2. ALLOCATION TO GOALS
        decimal remainingForGoals = investableSurplus;
        if (debtActions.Any())
        {
            remainingForGoals = Math.Max(0, investableSurplus - debtActions.Sum(d => d.SuggestedAmortization));
        }

        if (goals.Any())
        {
            // First goal (highest priority)
            var topGoal = goals.First();
            decimal suggestedGoalDeposit = 0;

            if (remainingForGoals > 0)
            {
                suggestedGoalDeposit = Math.Min(topGoal.TargetAmount - topGoal.CurrentAmount, remainingForGoals);
            }

            // Estimate months remaining
            int monthsRemaining = 12;
            if (topGoal.TargetDate.HasValue)
            {
                var diff = ((topGoal.TargetDate.Value.Year - year) * 12) + (topGoal.TargetDate.Value.Month - month);
                monthsRemaining = Math.Max(1, diff);
            }

            decimal remainingGoalNeeded = Math.Max(0, topGoal.TargetAmount - topGoal.CurrentAmount);
            decimal requiredPerMonth = Math.Round(remainingGoalNeeded / monthsRemaining, 2);

            goalActions.Add(new GoalRecommendationDto
            {
                GoalId = topGoal.Id,
                GoalName = topGoal.Name,
                TargetAmount = topGoal.TargetAmount,
                CurrentAmount = topGoal.CurrentAmount,
                SuggestedDeposit = suggestedGoalDeposit,
                MonthsAhead = suggestedGoalDeposit > requiredPerMonth ? 1 : 0,
                StrategyReason = suggestedGoalDeposit > 0
                    ? $"Aportar R$ {suggestedGoalDeposit:N2} cobrirá o aporte do mês e manterá sua meta no prazo previsto."
                    : $"Meta prioritária: requer em média R$ {requiredPerMonth:N2}/mês para ser batida até {topGoal.TargetDate:MM/yyyy}."
            });

            // Other goals
            foreach (var g in goals.Skip(1))
            {
                goalActions.Add(new GoalRecommendationDto
                {
                    GoalId = g.Id,
                    GoalName = g.Name,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    SuggestedDeposit = 0,
                    MonthsAhead = 0,
                    StrategyReason = "Aguardando conclusão ou aporte da meta de prioridade superior."
                });
            }
        }

        // 3. SYNTHESIZE HEALTH SCORE & PRIMARY RECOMMENDATION
        if (projectedSurplus < 0)
        {
            healthStatus = "Atenção (Déficit Projetado)";
            healthBadgeClass = "bg-danger";
            recTitle = "Contenção de Despesas para Evitar Déficit";
            recMessage = $"Seu mês projeta um déficit de R$ {Math.Abs(projectedSurplus):N2}. Recomendamos reavaliar gastos diários ou renegociar despesas fixas para fechar no azul.";
            tips.Add($"Seu teto diário seguro ideal para equilibrar o mês é de até R$ {safeDailyTarget:N2}/dia.");
            tips.Add("Evite novas compras parceladas no cartão de crédito neste momento.");
        }
        else if (debts.Any(d => d.InterestRateMonthly >= 3.0m))
        {
            var worstDebt = debts.First(d => d.InterestRateMonthly >= 3.0m);
            healthStatus = "Ação Necessária (Juros Elevados)";
            healthBadgeClass = "bg-warning text-dark";
            recTitle = $"Prioridade: Quitar {worstDebt.Name} ({worstDebt.InterestRateMonthly}% a.m.)";
            recMessage = $"Você possui uma previsão de sobra de R$ {projectedSurplus:N2}. A atitude matematicamente mais rentável é destinar R$ {debtActions.FirstOrDefault()?.SuggestedAmortization ?? 0:N2} para amortizar {worstDebt.Name}, eliminando juros que corroem seu patrimônio.";
            tips.Add($"Cada R$ 100 amortizados em {worstDebt.Name} economizam R$ {worstDebt.InterestRateMonthly * 12:N2} em juros no ano.");
            tips.Add("Após quitar dívidas com juros > 3%, sua capacidade de poupança mensal aumentará significativamente.");
        }
        else if (debts.Any(d => d.InterestRateMonthly > 0))
        {
            var worstDebt = debts.First(d => d.InterestRateMonthly > 0);
            healthStatus = "Amortização Recomendada";
            healthBadgeClass = "bg-warning text-dark";
            recTitle = $"Amortizar Dívida com Juros: {worstDebt.Name}";
            recMessage = $"Destinar R$ {debtActions.FirstOrDefault()?.SuggestedAmortization ?? 0:N2} para amortizar {worstDebt.Name} ({worstDebt.InterestRateMonthly}% a.m.) evitará o acúmulo de juros.";
            tips.Add("Eliminar dívidas com juros é o investimento mais seguro e rentável a curto prazo.");
        }
        else if (debts.Any())
        {
            // All debts are 0% interest
            var smallestDebt = zeroInterestDebts.First();
            var primeGoal = goals.FirstOrDefault();

            healthStatus = "Equilibrado (Dívidas Controladas)";
            healthBadgeClass = "bg-primary";

            if (primeGoal != null && investableSurplus > 0)
            {
                var suggestedDebtAmt = debtActions.FirstOrDefault()?.SuggestedAmortization ?? 0;
                var suggestedGoalAmt = goalActions.FirstOrDefault()?.SuggestedDeposit ?? 0;

                recTitle = "Estratégia Híbrida: Bola de Neve & Metas";
                recMessage = $"Você possui {debts.Count} dívidas somando R$ {totalDebtsRemaining:N2} (sem juros ativos). Com a sobra prevista de R$ {projectedSurplus:N2}, sugerimos alocar R$ {suggestedDebtAmt:N2} para amortizar a menor dívida ({smallestDebt.Name}) e R$ {suggestedGoalAmt:N2} na meta '{primeGoal.Name}'.";
            }
            else if (investableSurplus > 0)
            {
                var suggestedDebtAmt = debtActions.FirstOrDefault()?.SuggestedAmortization ?? 0;
                recTitle = $"Acelerar Quitação: {smallestDebt.Name} (Bola de Neve)";
                recMessage = $"Com a sobra prevista de R$ {projectedSurplus:N2}, sugerimos antecipar R$ {suggestedDebtAmt:N2} na dívida {smallestDebt.Name} (saldo de R$ {smallestDebt.RemainingAmount:N2}) para eliminá-la e reduzir suas faturas mensais.";
            }
            else
            {
                recTitle = "Dívidas Controladas sem Juros";
                recMessage = $"Suas {debts.Count} dívidas cadastradas estão sem incidência de juros ativos. Mantenha o pagamento pontual das parcelas para preservar seu histórico financeiro.";
            }

            tips.Add("Seus cartões/dívidas estão cadastrados com 0% a.m. Caso algum entre no rotativo ou cheque especial, edite a dívida para informar a taxa real e recalcular o plano.");
            tips.Add($"Pelo Método Bola de Neve, quitar {smallestDebt.Name} primeiro elimina uma conta inteira da sua rotina.");
            if (primeGoal != null)
            {
                tips.Add($"Você pode equilibrar amortização antecipada com aportes na meta '{primeGoal.Name}'.");
            }
        }
        else if (projectedSurplus > 0 && goals.Any())
        {
            var primeGoal = goals.First();
            healthStatus = "Excelente (Sobra para Investir)";
            healthBadgeClass = "bg-success";
            recTitle = $"Acelerar Meta: {primeGoal.Name}";
            recMessage = $"Parabéns! Sua gestão está gerando uma sobra prevista de R$ {projectedSurplus:N2}. Sugerimos alocar R$ {goalActions.FirstOrDefault()?.SuggestedDeposit ?? projectedSurplus:N2} na meta '{primeGoal.Name}'.";
            tips.Add($"Você atingirá sua meta '{primeGoal.Name}' com folga se mantiver seus gastos diários em torno de R$ {safeDailyTarget:N2}/dia.");
            tips.Add("Considere manter parte da sobra em uma aplicação de liquidez diária como Reserva de Emergência.");
        }
        else
        {
            healthStatus = "Estável";
            healthBadgeClass = "bg-info text-dark";
            recTitle = "Fluxo Positivo e Equilibrado";
            recMessage = $"Suas contas estão equilibradas com saldo projetado positivo de R$ {projectedSurplus:N2}. Cadastre suas metas e dívidas para receber planos personalizados de aceleração patrimonial.";
            tips.Add("Cadastre uma meta como 'Reserva de Emergência' para acompanhar seu colchão de segurança.");
        }

        return new AdvisorAnalysisDto
        {
            Year = year,
            Month = month,
            MonthName = monthlyFlow.MonthName,
            ProjectedSurplus = projectedSurplus,
            CurrentRealizedSurplus = monthlyFlow.Performance,
            DailySafeSpendingTarget = safeDailyTarget,
            DaysRemainingInMonth = daysRemaining,
            HealthStatus = healthStatus,
            HealthBadgeClass = healthBadgeClass,
            PrimaryRecommendationTitle = recTitle,
            PrimaryRecommendationMessage = recMessage,
            TotalDebtsRemaining = totalDebtsRemaining,
            TotalMonthlyInterestCost = totalMonthlyInterestCost,
            TotalGoalsTarget = totalGoalsTarget,
            TotalGoalsCurrent = totalGoalsCurrent,
            DebtActions = debtActions,
            GoalActions = goalActions,
            SmartTips = tips
        };
    }
}
