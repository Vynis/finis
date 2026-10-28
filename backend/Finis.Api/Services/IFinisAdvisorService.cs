using Finis.Api.DTOs;

namespace Finis.Api.Services;

public interface IFinisAdvisorService
{
    Task<AdvisorAnalysisDto> GetAdvisorAnalysisAsync(int userId, int year, int month, int? accountId = null);
}
