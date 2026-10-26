using Finis.Api.DTOs;

namespace Finis.Api.Services;

public interface ITermometroEngineService
{
    Task<MonthlyFlowResponseDto> GetMonthlyFlowAsync(int userId, int year, int month);
    Task<AnnualEconomyResponseDto> GetAnnualEconomyAsync(int userId, int year);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, int year, int month);
    Task<decimal> GetCarryOverBalanceAsync(int userId, DateTime targetMonthStart);
}
