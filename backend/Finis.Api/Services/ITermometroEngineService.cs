using Finis.Api.DTOs;

namespace Finis.Api.Services;

public interface ITermometroEngineService
{
    Task<MonthlyFlowResponseDto> GetMonthlyFlowAsync(int userId, int year, int month, int? accountId = null);
    Task<AnnualEconomyResponseDto> GetAnnualEconomyAsync(int userId, int year, int? accountId = null);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(int userId, int year, int month, int? accountId = null);
    Task<decimal> GetCarryOverBalanceAsync(int userId, DateTime targetMonthStart, int? accountId = null);
}
